/*
 * MixamoClient.cs
 * 
 * HTTP client for Adobe Mixamo API communication.
 * Features:
 * - Encrypted token storage
 * - Rate limiting (2-5 second delays)
 * - Exponential backoff retry (max 3 attempts)
 * - Progress monitoring for exports
 * 
 * Part of Unity-MCP Mixamo Animation Auto-Fetch System
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using com.IvanMurzak.ReflectorNet.Utils;

namespace MCPForUnity.Editor.Mixamo
{
    /// <summary>
    /// HTTP client for Mixamo API communication with retry logic and rate limiting.
    /// </summary>
    public class MixamoClient : IDisposable
    {
        #region Singleton
        
        private static MixamoClient _instance;
        private static readonly object _lock = new object();
        
        public static MixamoClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MixamoClient();
                        }
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Fields
        
        private HttpClient _httpClient;
        private readonly System.Random _random = new System.Random();
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        
        // Token management
        private const string TOKEN_KEY = "MixamoEncryptedToken";
        private const string TOKEN_SALT_KEY = "MixamoTokenSalt";
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("MCPForUnity_Mixamo_2024");
        
        // Cached token for thread-safe access
        private string _cachedToken = null;
        private bool _tokenLoaded = false;
        private readonly object _tokenLock = new object();
        
        #endregion
        
        #region Constructor
        
        private MixamoClient()
        {
            InitializeHttpClient();
        }
        
        private void InitializeHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", MixamoConstants.API_KEY);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Unity-MCP/1.0");
            
            // Don't load token in constructor - will be loaded on demand via main thread
        }
        
        #endregion
        
        #region Token Management (Encrypted Storage)
        
        /// <summary>
        /// Ensure token is loaded from EditorPrefs (must be called from main thread or via MainThread helper).
        /// </summary>
        private void EnsureTokenLoaded()
        {
            lock (_tokenLock)
            {
                if (_tokenLoaded) return;
                
                // This must run on main thread
                MainThread.Instance.Run(() =>
                {
                    _cachedToken = LoadTokenFromPrefs();
                    _tokenLoaded = true;
                    
                    if (!string.IsNullOrEmpty(_cachedToken))
                    {
                        SetAuthorizationHeader(_cachedToken);
                    }
                });
            }
        }
        
        /// <summary>
        /// Load token from EditorPrefs (must be called on main thread).
        /// </summary>
        private string LoadTokenFromPrefs()
        {
            if (!EditorPrefs.HasKey(TOKEN_KEY) || !EditorPrefs.HasKey(TOKEN_SALT_KEY))
            {
                return null;
            }
            
            try
            {
                var obfuscatedToken = EditorPrefs.GetString(TOKEN_KEY);
                var saltBase64 = EditorPrefs.GetString(TOKEN_SALT_KEY);
                var salt = Convert.FromBase64String(saltBase64);
                
                return DeobfuscateToken(obfuscatedToken, salt);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MixamoClient] Failed to retrieve token: {ex.Message}");
                ClearTokenInternal();
                return null;
            }
        }
        
        /// <summary>
        /// Store authentication token with encryption.
        /// </summary>
        public void StoreToken(string token)
        {
            MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(token))
                {
                    ClearTokenInternal();
                    return;
                }
                
                try
                {
                    // Generate random salt
                    var salt = new byte[16];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(salt);
                    }
                    
                    // Obfuscate token (XOR-based, simple but effective for local storage)
                    var obfuscated = ObfuscateToken(token, salt);
                    EditorPrefs.SetString(TOKEN_KEY, obfuscated);
                    EditorPrefs.SetString(TOKEN_SALT_KEY, Convert.ToBase64String(salt));
                    
                    // Update cache
                    lock (_tokenLock)
                    {
                        _cachedToken = token;
                        _tokenLoaded = true;
                    }
                    
                    SetAuthorizationHeader(token);
                    Debug.Log("[MixamoClient] Token stored successfully");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MixamoClient] Failed to store token: {ex.Message}");
                    throw;
                }
            });
        }
        
        /// <summary>
        /// Retrieve stored authentication token (thread-safe).
        /// </summary>
        public string GetStoredToken()
        {
            EnsureTokenLoaded();
            lock (_tokenLock)
            {
                return _cachedToken;
            }
        }
        
        /// <summary>
        /// Clear stored token.
        /// </summary>
        public void ClearToken()
        {
            MainThread.Instance.Run(() => ClearTokenInternal());
        }
        
        private void ClearTokenInternal()
        {
            EditorPrefs.DeleteKey(TOKEN_KEY);
            EditorPrefs.DeleteKey(TOKEN_SALT_KEY);
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            lock (_tokenLock)
            {
                _cachedToken = null;
                _tokenLoaded = true; // Mark as loaded (with null value)
            }
        }
        
        /// <summary>
        /// Check if a valid token is stored (thread-safe).
        /// </summary>
        public bool HasToken
        {
            get
            {
                EnsureTokenLoaded();
                lock (_tokenLock)
                {
                    return !string.IsNullOrEmpty(_cachedToken);
                }
            }
        }
        
        private void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        private static byte[] CombineArrays(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }
        
        private string ObfuscateToken(string token, byte[] salt)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            for (int i = 0; i < tokenBytes.Length; i++)
            {
                tokenBytes[i] ^= salt[i % salt.Length];
                tokenBytes[i] ^= _entropy[i % _entropy.Length];
            }
            return Convert.ToBase64String(tokenBytes);
        }
        
        private string DeobfuscateToken(string obfuscated, byte[] salt)
        {
            var tokenBytes = Convert.FromBase64String(obfuscated);
            for (int i = 0; i < tokenBytes.Length; i++)
            {
                tokenBytes[i] ^= _entropy[i % _entropy.Length];
                tokenBytes[i] ^= salt[i % salt.Length];
            }
            return Encoding.UTF8.GetString(tokenBytes);
        }
        
        #endregion
        
        #region Token Validation
        
        /// <summary>
        /// Validate token by making a test API call to an authenticated endpoint.
        /// Note: Search API (/products) does NOT require authentication, so we use /characters endpoint.
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string token = null)
        {
            var testToken = token ?? GetStoredToken();
            if (string.IsNullOrEmpty(testToken))
            {
                return false;
            }
            
            // Temporarily set the token for testing
            var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", testToken);
            
            try
            {
                // Use characters endpoint which requires authentication
                // This endpoint returns user's uploaded characters
                var url = $"{MixamoConstants.BASE_URL}/characters";
                var response = await _httpClient.GetAsync(url);
                
                // 401/403 means token is invalid/expired
                // 200 or even 404 means token is valid (authenticated but may have no characters)
                return response.StatusCode != HttpStatusCode.Unauthorized && 
                       response.StatusCode != HttpStatusCode.Forbidden;
            }
            catch
            {
                return false;
            }
            finally
            {
                _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
            }
        }
        
        #endregion
        
        #region Rate Limiting
        
        /// <summary>
        /// Apply rate limiting delay (2-5 seconds between requests).
        /// </summary>
        private async Task ApplyRateLimitAsync()
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                var minDelay = TimeSpan.FromMilliseconds(MixamoConstants.MIN_REQUEST_DELAY_MS);
                
                if (timeSinceLastRequest < minDelay)
                {
                    var randomDelay = _random.Next(
                        MixamoConstants.MIN_REQUEST_DELAY_MS,
                        MixamoConstants.MAX_REQUEST_DELAY_MS
                    );
                    await Task.Delay(randomDelay);
                }
                
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }
        
        #endregion
        
        #region Retry Logic
        
        /// <summary>
        /// Execute HTTP request with exponential backoff retry.
        /// </summary>
        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
            Func<Task<HttpResponseMessage>> requestFunc,
            CancellationToken cancellationToken = default)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt < MixamoConstants.MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    await ApplyRateLimitAsync();
                    
                    var response = await requestFunc();
                    
                    // Check for rate limiting
                    if (response.StatusCode == (HttpStatusCode)429)
                    {
                        var retryAfter = GetRetryAfterSeconds(response);
                        Debug.LogWarning($"[MixamoClient] Rate limited. Waiting {retryAfter}s...");
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter), cancellationToken);
                        continue;
                    }
                    
                    // Success or non-retryable error
                    if (response.IsSuccessStatusCode || 
                        response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return response;
                    }
                    
                    // Server error - retry with backoff
                    lastException = new HttpRequestException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new TimeoutException("Request timed out");
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                }
                
                if (attempt < MixamoConstants.MAX_RETRY_ATTEMPTS - 1)
                {
                    var delay = CalculateBackoffDelay(attempt);
                    Debug.LogWarning($"[MixamoClient] Request failed, retrying in {delay}ms (attempt {attempt + 1}/{MixamoConstants.MAX_RETRY_ATTEMPTS})");
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            throw lastException ?? new Exception("Max retry attempts exceeded");
        }
        
        private int CalculateBackoffDelay(int attempt)
        {
            // Exponential backoff: 1s, 2s, 4s...
            var baseDelay = MixamoConstants.BASE_RETRY_DELAY_MS * Math.Pow(2, attempt);
            // Add jitter
            var jitter = _random.Next(0, 500);
            return (int)baseDelay + jitter;
        }
        
        private int GetRetryAfterSeconds(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                var retryAfter = string.Join("", values);
                if (int.TryParse(retryAfter, out var seconds))
                {
                    return seconds;
                }
            }
            return 5; // Default 5 seconds
        }
        
        #endregion
        
        #region API Methods
        
        /// <summary>
        /// Search for animations by keyword.
        /// </summary>
        public async Task<MixamoSearchResponse> SearchAnimationsAsync(
            string keyword,
            int limit = 20,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            
            var encodedKeyword = Uri.EscapeDataString(keyword);
            var url = $"{MixamoConstants.BASE_URL}/products?type=Motion&query={encodedKeyword}&limit={limit}&page={page}";
            
            var response = await ExecuteWithRetryAsync(
                () => _httpClient.GetAsync(url, cancellationToken),
                cancellationToken
            );
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonUtility.FromJson<MixamoSearchResponse>(json);
        }
        
        /// <summary>
        /// Get animation product details including gms_hash.
        /// </summary>
        public async Task<MixamoProductResponse> GetProductAsync(
            string animationId,
            string characterId = null,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            
            characterId = characterId ?? MixamoConstants.DEFAULT_CHARACTER_ID;
            var url = $"{MixamoConstants.BASE_URL}/products/{animationId}?similar=0&character_id={characterId}";
            
            var response = await ExecuteWithRetryAsync(
                () => _httpClient.GetAsync(url, cancellationToken),
                cancellationToken
            );
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            // Preprocess JSON to handle hyphenated keys (model-id, arm-space)
            json = PreprocessJsonForParsing(json);
            return JsonUtility.FromJson<MixamoProductResponse>(json);
        }
        
        /// <summary>
        /// Request animation export.
        /// </summary>
        public async Task<MixamoExportResponse> ExportAnimationAsync(
            MixamoExportRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            
            var url = $"{MixamoConstants.BASE_URL}/animations/export";
            var json = JsonUtility.ToJson(request);
            // Postprocess JSON to convert underscores back to hyphens for API
            json = PostprocessJsonForApi(json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await ExecuteWithRetryAsync(
                () => _httpClient.PostAsync(url, content, cancellationToken),
                cancellationToken
            );
            
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonUtility.FromJson<MixamoExportResponse>(responseJson);
        }
        
        /// <summary>
        /// Monitor export progress until completion.
        /// </summary>
        public async Task<string> MonitorExportAsync(
            string characterId,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            
            var url = $"{MixamoConstants.BASE_URL}/characters/{characterId}/monitor";
            
            for (int i = 0; i < MixamoConstants.MONITOR_MAX_ATTEMPTS; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var response = await ExecuteWithRetryAsync(
                    () => _httpClient.GetAsync(url, cancellationToken),
                    cancellationToken
                );
                
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var status = JsonUtility.FromJson<MixamoMonitorResponse>(json);
                
                switch (status.status)
                {
                    case "completed":
                        progress?.Report("Export completed");
                        return status.job_result; // Download URL
                        
                    case "processing":
                        progress?.Report($"Processing... ({i + 1}/{MixamoConstants.MONITOR_MAX_ATTEMPTS})");
                        await Task.Delay(MixamoConstants.MONITOR_POLL_INTERVAL_MS, cancellationToken);
                        break;
                        
                    case "failed":
                        throw new Exception($"Export failed: {status.error?.message ?? "Unknown error"}");
                        
                    default:
                        throw new Exception($"Unknown export status: {status.status}");
                }
            }
            
            throw new TimeoutException("Export monitoring timed out");
        }
        
        /// <summary>
        /// Download FBX file from URL.
        /// </summary>
        public async Task<string> DownloadFbxAsync(
            string downloadUrl,
            string savePath,
            string fileName,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            // Ensure directory exists
            var fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), savePath);
            Directory.CreateDirectory(fullPath);
            
            var filePath = Path.Combine(fullPath, $"{SanitizeFileName(fileName)}.fbx");
            
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadedBytes = 0L;
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        downloadedBytes += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            progress?.Report((float)downloadedBytes / totalBytes);
                        }
                    }
                }
            }
            
            // Convert to Unity asset path
            var assetPath = "Assets" + filePath.Replace(Application.dataPath, "").Replace("\\", "/");
            return assetPath;
        }
        
        /// <summary>
        /// Upload character for auto-rigging.
        /// </summary>
        public async Task<MixamoUploadResponse> UploadCharacterAsync(
            byte[] fbxData,
            string fileName,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            
            var url = $"{MixamoConstants.BASE_URL}/characters";
            
            using (var formData = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(fbxData);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                formData.Add(fileContent, "file", fileName);
                
                progress?.Report("Uploading character...");
                
                var response = await ExecuteWithRetryAsync(
                    () => _httpClient.PostAsync(url, formData, cancellationToken),
                    cancellationToken
                );
                
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonUtility.FromJson<MixamoUploadResponse>(json);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private void EnsureAuthenticated()
        {
            if (!HasToken)
            {
                throw new InvalidOperationException(
                    "Mixamo authentication required. Use 'mixamo-auth' tool to set your token.\n" +
                    "Get your token from mixamo.com browser console: localStorage.access_token"
                );
            }
        }
        
        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder(fileName);
            foreach (var c in invalid)
            {
                sanitized.Replace(c, '_');
            }
            return sanitized.ToString().Replace(' ', '_');
        }
        
        /// <summary>
        /// Preprocess JSON to convert hyphenated keys to underscores for Unity JsonUtility compatibility.
        /// Mixamo API uses keys like "model-id" and "arm-space" which JsonUtility cannot parse.
        /// </summary>
        private static string PreprocessJsonForParsing(string json)
        {
            return json
                .Replace("\"model-id\"", "\"model_id\"")
                .Replace("\"arm-space\"", "\"arm_space\"");
        }
        
        /// <summary>
        /// Postprocess JSON to convert underscored keys back to hyphens for Mixamo API compatibility.
        /// </summary>
        private static string PostprocessJsonForApi(string json)
        {
            return json
                .Replace("\"model_id\"", "\"model-id\"")
                .Replace("\"arm_space\"", "\"arm-space\"");
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            _httpClient?.Dispose();
            _rateLimitSemaphore?.Dispose();
        }
        
        #endregion
    }
}
