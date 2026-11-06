using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;

/// <summary>
/// Quản lý Bảng Xếp Hạng Trực Tuyến qua Unity Gaming Services (UGS).
/// - Tự động khởi tạo dịch vụ & đăng nhập ẩn danh (Anonymous Sign-In).
/// - Quản lý tên người chơi (PlayerName).
/// - Gửi điểm số (SubmitScore) lên Leaderboard ID "High_Score".
/// - Kéo danh sách Top kỷ lục (GetTopScores) và Thứ hạng bản thân (GetMyScore).
/// </summary>
public class UGSLeaderboardManager : MonoBehaviour
{
    private static UGSLeaderboardManager _instance;
    public static UGSLeaderboardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UGSLeaderboardManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UGSLeaderboardManager");
                    _instance = go.AddComponent<UGSLeaderboardManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Leaderboard Settings")]
    [Tooltip("ID của Leaderboard trên Unity Cloud Dashboard. Phải khớp 100% từng chữ.")]
    public string leaderboardId = "High_Score";

    [Header("Status (Read-Only)")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private string currentPlayerId = "";
    [SerializeField] private string currentPlayerName = "";

    public bool IsReady => isInitialized && AuthenticationService.Instance.IsSignedIn;
    public string PlayerId => currentPlayerId;
    public string PlayerName => currentPlayerName;

    public event Action OnInitialized;
    public event Action<string> OnPlayerNameChanged;

    private async void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeServicesAsync();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private static Task<bool> _initializationTask = null;

    /// <summary>
    /// Khởi tạo Unity Services và đăng nhập ẩn danh cho thiết bị
    /// </summary>
    public async Task<bool> InitializeServicesAsync()
    {
        if (IsReady) return true;

        if (_initializationTask != null && !_initializationTask.IsCompleted)
        {
            return await _initializationTask;
        }

        _initializationTask = DoInitializeServicesAsync();
        return await _initializationTask;
    }

    private async Task<bool> DoInitializeServicesAsync()
    {
        try
        {
            // 1. Chờ nếu dịch vụ đang trong tiến trình khởi tạo
            while (UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            // Khởi tạo UGS Core nếu chưa
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // 2. Đăng nhập ẩn danh nếu chưa đăng nhập
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            currentPlayerId = AuthenticationService.Instance.PlayerId;

            // 3. Đọc tên người chơi đã lưu (nếu có)
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            currentPlayerName = savedName;

            if (!string.IsNullOrEmpty(savedName))
            {
                // Cập nhật tên lên Unity Authentication nếu đã lưu từ trước
                try
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(savedName);
                }
                catch
                {
                    // Bỏ qua nếu offline hoặc tên chưa cập nhật được
                }
            }

            isInitialized = true;
            Debug.Log($"<color=#4ade80>[UGS Leaderboard]</color> Khởi tạo thành công! PlayerID: {currentPlayerId}, Name: {(string.IsNullOrEmpty(currentPlayerName) ? "(Chưa đặt tên)" : currentPlayerName)}");
            OnInitialized?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            isInitialized = false;
            Debug.LogError($"<color=#f87171>[UGS Leaderboard] Lỗi khởi tạo:</color> {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Đổi tên người chơi và đồng bộ lên server
    /// </summary>
    public async Task<bool> UpdatePlayerNameAsync(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return false;

        newName = newName.Trim();
        if (!IsReady)
        {
            bool ready = await InitializeServicesAsync();
            if (!ready || !IsReady) return false;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            currentPlayerName = newName;
            PlayerPrefs.SetString("PlayerName", newName);
            PlayerPrefs.Save();

            Debug.Log($"<color=#38bdf8>[UGS Leaderboard]</color> Cập nhật tên thành công: {newName}");
            OnPlayerNameChanged?.Invoke(newName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#f87171>[UGS Leaderboard] Lỗi cập nhật tên:</color> {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gửi điểm số lên Leaderboard (Unity sẽ tự giữ điểm cao nhất nếu chọn Best Score trên Dashboard)
    /// </summary>
    public async Task<LeaderboardEntry> SubmitScoreAsync(int score)
    {
        if (score <= 0) return null;

        if (!IsReady)
        {
            bool ready = await InitializeServicesAsync();
            if (!ready || !IsReady)
            {
                Debug.LogWarning("[UGS Leaderboard] Chưa kết nối được dịch vụ để gửi điểm.");
                return null;
            }
        }

        try
        {
            var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"<color=#4ade80>[UGS Leaderboard] Gửi điểm thành công!</color> Điểm: {score} | Hạng: #{entry.Rank + 1}");
            return entry;
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#f87171>[UGS Leaderboard] Lỗi gửi điểm:</color> {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lấy danh sách Top người chơi cao nhất (mặc định lấy 10 người)
    /// </summary>
    public async Task<List<LeaderboardEntry>> GetTopScoresAsync(int limit = 10)
    {
        if (!IsReady)
        {
            bool ready = await InitializeServicesAsync();
            if (!ready || !IsReady) return null;
        }

        try
        {
            var options = new GetScoresOptions { Limit = limit };
            var scoresPage = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            return scoresPage.Results;
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#f87171>[UGS Leaderboard] Lỗi lấy danh sách Top:</color> {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lấy thứ hạng và điểm số kỷ lục của chính bản thân người chơi
    /// </summary>
    public async Task<LeaderboardEntry> GetMyScoreAsync()
    {
        if (!IsReady)
        {
            bool ready = await InitializeServicesAsync();
            if (!ready || !IsReady) return null;
        }

        try
        {
            var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            return entry;
        }
        catch (LeaderboardsException lex) when (lex.Reason == LeaderboardsExceptionReason.EntryNotFound)
        {
            // Người chơi mới, chưa từng gửi điểm lần nào
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UGS Leaderboard] Chưa lấy được điểm cá nhân: {ex.Message}");
            return null;
        }
    }
}
