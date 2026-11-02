using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FashionWarehouse.WpfApp.Views;

public partial class CategoryView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(5)
    };

    private List<CategoryItem> _categories = new();
    private int? _editingId;

    private static readonly List<CategoryItem> FallbackData = new()
    {
        new() { CategoryId = 1, CategoryName = "Áo Thun / T-Shirt",  Description = "Áo thun, áo polo các chất liệu", ProductCount = 5 },
        new() { CategoryId = 2, CategoryName = "Áo Sơ Mi",           Description = "Sơ mi công sở, sơ mi casual",   ProductCount = 3 },
        new() { CategoryId = 3, CategoryName = "Quần Jeans",          Description = "Quần jeans nam nữ các kiểu dáng", ProductCount = 2 },
        new() { CategoryId = 4, CategoryName = "Quần Tây / Kaki",     Description = "Quần tây công sở, quần kaki",   ProductCount = 1 },
        new() { CategoryId = 5, CategoryName = "Phụ kiện",            Description = "Thắt lưng, túi, ví, mũ nón",   ProductCount = 0 }
    };

    public CategoryView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadCategoriesAsync();
    }

    private async void LoadCategoriesAsync()
    {
        try
        {
            var data = await HttpClient.GetFromJsonAsync<List<CategoryItem>>("api/categories");
            _categories = (data != null && data.Count > 0) ? data : new List<CategoryItem>(FallbackData);
        }
        catch
        {
            _categories = new List<CategoryItem>(FallbackData);
        }

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        dgCategories.ItemsSource = null;
        dgCategories.ItemsSource = _categories;
        txtCount.Text = $"{_categories.Count} danh mục";
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadCategoriesAsync();

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = txtCategoryName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowMessage("Vui lòng nhập tên danh mục.", false);
            txtCategoryName.Focus();
            return;
        }

        var body = new { CategoryName = name, Description = txtDescription.Text.Trim() };

        try
        {
            if (_editingId.HasValue)
            {
                var response = await HttpClient.PutAsJsonAsync($"api/categories/{_editingId.Value}", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã cập nhật danh mục '{name}'.", true);
                    ExitEditMode();
                    LoadCategoriesAsync();
                    return;
                }
                var err = await response.Content.ReadAsStringAsync();
                ShowMessage($"Lỗi cập nhật: {err}", false);
            }
            else
            {
                var response = await HttpClient.PostAsJsonAsync("api/categories", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã thêm danh mục '{name}'.", true);
                    ClearForm();
                    LoadCategoriesAsync();
                    return;
                }
                var err = await response.Content.ReadAsStringAsync();
                ShowMessage($"Lỗi thêm: {err}", false);
            }
        }
        catch
        {
            // Offline fallback: thao tác trực tiếp trên danh sách in-memory
            if (_editingId.HasValue)
            {
                var existing = _categories.FirstOrDefault(c => c.CategoryId == _editingId.Value);
                if (existing != null)
                {
                    existing.CategoryName = name;
                    existing.Description = body.Description;
                }
                ShowMessage($"✓ Đã cập nhật '{name}' (offline).", true);
                ExitEditMode();
            }
            else
            {
                int newId = _categories.Count > 0 ? _categories.Max(c => c.CategoryId) + 1 : 1;
                _categories.Add(new CategoryItem
                {
                    CategoryId = newId,
                    CategoryName = name,
                    Description = body.Description,
                    ProductCount = 0
                });
                ShowMessage($"✓ Đã thêm '{name}' (offline).", true);
                ClearForm();
            }
            RefreshGrid();
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
            StartEdit(id);
    }

    private void DgCategories_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgCategories.SelectedItem is CategoryItem item)
            StartEdit(item.CategoryId);
    }

    private void StartEdit(int id)
    {
        var item = _categories.FirstOrDefault(c => c.CategoryId == id);
        if (item == null) return;

        _editingId = id;
        txtCategoryName.Text = item.CategoryName;
        txtDescription.Text = item.Description ?? string.Empty;
        txtSaveButton.Text = "💾 Lưu Thay Đổi";
        btnCancel.Visibility = Visibility.Visible;
        brdMessage.Visibility = Visibility.Collapsed;
        txtCategoryName.Focus();
        txtCategoryName.SelectAll();
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int id) return;

        var item = _categories.FirstOrDefault(c => c.CategoryId == id);
        if (item == null) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa danh mục '{item.CategoryName}'?\n\nLưu ý: Không thể xóa danh mục đang chứa sản phẩm.",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var response = await HttpClient.DeleteAsync($"api/categories/{id}");
            if (response.IsSuccessStatusCode)
            {
                ShowMessage($"✓ Đã xóa '{item.CategoryName}'.", true);
                LoadCategoriesAsync();
                return;
            }
            var err = await response.Content.ReadAsStringAsync();
            ShowMessage($"Không thể xóa: {err}", false);
        }
        catch
        {
            if (item.ProductCount > 0)
            {
                ShowMessage("Không thể xóa danh mục đang chứa sản phẩm.", false);
                return;
            }
            _categories.Remove(item);
            ShowMessage($"✓ Đã xóa '{item.CategoryName}' (offline).", true);
            RefreshGrid();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        ExitEditMode();
        ClearForm();
        brdMessage.Visibility = Visibility.Collapsed;
    }

    private void ExitEditMode()
    {
        _editingId = null;
        txtSaveButton.Text = "+ Thêm Danh Mục";
        btnCancel.Visibility = Visibility.Collapsed;
    }

    private void ClearForm()
    {
        txtCategoryName.Clear();
        txtDescription.Clear();
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        txtMessage.Text = message;
        brdMessage.Background = new SolidColorBrush(isSuccess
            ? Color.FromRgb(240, 253, 244)
            : Color.FromRgb(254, 242, 242));
        txtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(22, 101, 52)
            : Color.FromRgb(185, 28, 28));
        brdMessage.Visibility = Visibility.Visible;
    }

    public sealed class CategoryItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }
}
