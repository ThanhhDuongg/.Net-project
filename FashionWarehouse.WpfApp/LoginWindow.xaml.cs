using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp;

public partial class LoginWindow : Window
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116"),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public LoginWindow()
    {
        InitializeComponent();
        txtUsername.Focus();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnLogin_Click(sender, e);
        }
    }

    private void BtnQuickAdmin_Click(object sender, RoutedEventArgs e)
    {
        txtUsername.Text = "admin";
        txtPassword.Password = "password";
        brdAlert.Visibility = Visibility.Collapsed;
    }

    private void BtnQuickWarehouse_Click(object sender, RoutedEventArgs e)
    {
        txtUsername.Text = "thukho";
        txtPassword.Password = "password";
        brdAlert.Visibility = Visibility.Collapsed;
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Password;

        if (string.IsNullOrEmpty(username))
        {
            ShowAlert("Vui lòng nhập tên đăng nhập.");
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowAlert("Vui lòng nhập mật khẩu.");
            txtPassword.Focus();
            return;
        }

        btnLogin.IsEnabled = false;
        txtLoginButton.Text = "Đang xác thực...";
        brdAlert.Visibility = Visibility.Collapsed;

        try
        {
            var requestData = new { Username = username, Password = password };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync("api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResult>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResult != null)
                {
                    UserSession.Login(loginResult.UserId, loginResult.Username, loginResult.FullName, loginResult.RoleName);
                    
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                    return;
                }
            }

            // Xử lý khi API trả về mã lỗi 401 hoặc 400
            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                var errorDoc = JsonDocument.Parse(errorBody);
                if (errorDoc.RootElement.TryGetProperty("message", out var msgElement))
                {
                    ShowAlert(msgElement.GetString() ?? "Đăng nhập thất bại.");
                }
                else
                {
                    ShowAlert("Tên đăng nhập hoặc mật khẩu không chính xác.");
                }
            }
            catch
            {
                ShowAlert("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
        }
        catch (HttpRequestException)
        {
            // Trường hợp máy chủ API chưa khởi động hoặc không kết nối được
            // Cung cấp tính năng đăng nhập ngoại tuyến dự phòng cho tài khoản Seed chuẩn
            if ((username == "admin" && password == "password") || (username == "thukho" && password == "password"))
            {
                if (username == "admin")
                    UserSession.Login(1, "admin", "Quản Trị Viên (Offline)", "Admin");
                else
                    UserSession.Login(2, "thukho", "Trần Văn Kho (Offline)", "WarehouseKeeper");

                MessageBox.Show("Không kết nối được Web API máy chủ. Đang đăng nhập ở chế độ Offline mặc định.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
                return;
            }

            ShowAlert("Không thể kết nối đến máy chủ backend (http://localhost:5116). Vui lòng đảm bảo API đang chạy.");
        }
        catch (Exception ex)
        {
            ShowAlert($"Lỗi hệ thống: {ex.Message}");
        }
        finally
        {
            btnLogin.IsEnabled = true;
            txtLoginButton.Text = "ĐĂNG NHẬP";
        }
    }

    private void ShowAlert(string message)
    {
        txtAlert.Text = message;
        brdAlert.Visibility = Visibility.Visible;
    }

    private sealed class LoginResult
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}

