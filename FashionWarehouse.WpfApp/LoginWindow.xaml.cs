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
            BtnLogin_Click(sender, e);
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
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowAlert("Vui lòng nhập mật khẩu.");
            return;
        }

        btnLogin.IsEnabled = false;
        txtLoginButton.Text = "Đang xác thực...";
        brdAlert.Visibility = Visibility.Collapsed;

        try
        {
            // Ưu tiên gọi API nếu server đang chạy
            var requestData = new { Username = username, Password = password };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync("api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LoginResult>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                {
                    UserSession.Login(result.UserId, result.Username, result.FullName, result.RoleName);
                    OpenMainWindow();
                    return;
                }
            }
        }
        catch
        {
            // API không chạy hoặc lỗi mạng — tiếp tục fallback
        }

        // Fallback: xác thực cục bộ cho tài khoản seed mặc định
        if (TryLocalLogin(username, password))
            return;

        ShowAlert("Tên đăng nhập hoặc mật khẩu không đúng.\n💡 Dùng: admin / password  hoặc  thukho / password");
        btnLogin.IsEnabled = true;
        txtLoginButton.Text = "ĐĂNG NHẬP";
    }

    private bool TryLocalLogin(string username, string password)
    {
        if (password != "password") return false;

        if (username == "admin")
        {
            UserSession.Login(1, "admin", "Quản Trị Viên", "Admin");
            OpenMainWindow();
            return true;
        }

        if (username == "thukho")
        {
            UserSession.Login(2, "thukho", "Trần Văn Kho", "WarehouseKeeper");
            OpenMainWindow();
            return true;
        }

        return false;
    }

    private void OpenMainWindow()
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }

    private void ShowAlert(string message)
    {
        txtAlert.Text = message;
        brdAlert.Visibility = Visibility.Visible;
        btnLogin.IsEnabled = true;
        txtLoginButton.Text = "ĐĂNG NHẬP";
    }

    private sealed class LoginResult
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
