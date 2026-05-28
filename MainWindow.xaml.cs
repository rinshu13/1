using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Magazin
{
    public partial class MainWindow : Window
    {
        private const string ConnectionString = @"Data Source=localhost;Initial Catalog=Magazin;Integrated Security=True";

        private readonly DataTable productsTable = new DataTable();
        private readonly DataTable ordersTable = new DataTable();
        private readonly DataTable suppliersTable = new DataTable();
        private string currentRole = "Гость";
        private string currentUserName = "Гость";
        private string selectedPhotoPath = "Images/picture.png";
        private int? editingProductId;
        private int? editingOrderId;

        public MainWindow()
        {
            InitializeComponent();
            CbSort.SelectedIndex = 0;
            LoadDictionaries();
            LoadProducts();
            ApplyRole("Гость", "Гость");
        }

        private void Fill(string query, DataTable table)
        {
            table.Clear();
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                adapter.Fill(table);
            }
        }

        private void Execute(string query, Action<SqlCommand> addParameters)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                addParameters?.Invoke(command);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private object Scalar(string query, Action<SqlCommand> addParameters)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                addParameters?.Invoke(command);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        private void LoadDictionaries()
        {
            Fill("SELECT 0 AS ID, N'Все поставщики' AS Title, 0 AS SortOrder UNION ALL SELECT ID, Title, 1 AS SortOrder FROM dbo.Suppliers ORDER BY SortOrder, Title", suppliersTable);
            CbSupplierFilter.ItemsSource = suppliersTable.DefaultView;
            CbSupplierFilter.SelectedValue = 0;

            CbSupplier.ItemsSource = LoadView("SELECT ID, Title FROM dbo.Suppliers ORDER BY Title");
            CbManufacturer.ItemsSource = LoadView("SELECT ID, Title FROM dbo.Manufacturers ORDER BY Title");
            CbCategory.ItemsSource = LoadView("SELECT ID, Title FROM dbo.Categories ORDER BY Title");
            CbUnit.ItemsSource = LoadView("SELECT ID, Title FROM dbo.Units ORDER BY Title");
            CbOrderStatus.ItemsSource = LoadView("SELECT ID, Title FROM dbo.OrderStatuses ORDER BY Title");
            CbPickupPoint.ItemsSource = LoadView("SELECT ID, Address FROM dbo.PickupPoints ORDER BY ID");
        }

        private DataView LoadView(string query)
        {
            DataTable table = new DataTable();
            Fill(query, table);
            return table.DefaultView;
        }

        private void LoadProducts()
        {
            Fill("SELECT * FROM dbo.vProductList", productsTable);
            EnsureProductColumns();
            foreach (DataRow row in productsTable.Rows)
            {
                int discount = ToInt(row["Discount"]);
                int quantity = ToInt(row["Quantity"]);
                row["DisplayPhotoPath"] = File.Exists(Convert.ToString(row["PhotoPath"])) ? row["PhotoPath"] : "Images/picture.png";
                row["HasDiscount"] = discount > 0;
                row["HighDiscount"] = discount > 17;
                row["NoStock"] = quantity <= 0;
                row["DiscountText"] = discount > 0 ? "Действующая скидка " + discount + "%" : "Скидка отсутствует";
            }
            ApplyProductFilter();
        }

        private void EnsureProductColumns()
        {
            AddColumn("DisplayPhotoPath", typeof(string));
            AddColumn("HasDiscount", typeof(bool));
            AddColumn("HighDiscount", typeof(bool));
            AddColumn("NoStock", typeof(bool));
            AddColumn("DiscountText", typeof(string));
        }

        private void AddColumn(string name, Type type)
        {
            if (!productsTable.Columns.Contains(name))
            {
                productsTable.Columns.Add(name, type);
            }
        }

        private void LoadOrders()
        {
            Fill("SELECT * FROM dbo.vOrderList ORDER BY ID", ordersTable);
            DgOrders.ItemsSource = ordersTable.DefaultView;
            TbCount.Text = "Заказов: " + ordersTable.Rows.Count;
        }

        private void ApplyRole(string role, string fullName)
        {
            currentRole = role;
            currentUserName = fullName;
            LoginPanel.Visibility = Visibility.Collapsed;
            TbUserInfo.Text = fullName + " (" + role + ")";

            bool canSearch = role == "Менеджер" || role == "Администратор";
            bool isAdmin = role == "Администратор";
            bool canOrders = role == "Менеджер" || role == "Администратор";

            ProductToolsPanel.Visibility = canSearch ? Visibility.Visible : Visibility.Collapsed;
            BtnAddProduct.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnOrdersView.Visibility = canOrders ? Visibility.Visible : Visibility.Collapsed;
            BtnAddOrder.Visibility = Visibility.Collapsed;
            ApplyProductFilter();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            DataTable table = new DataTable();
            Fill("SELECT * FROM dbo.vLogin", table);
            foreach (DataRow row in table.Rows)
            {
                if (Convert.ToString(row["Login"]) == TbLogin.Text.Trim() &&
                    Convert.ToString(row["Password"]) == PbPassword.Password.Trim())
                {
                    ApplyRole(Convert.ToString(row["RoleTitle"]), Convert.ToString(row["FullName"]));
                    return;
                }
            }
            MessageBox.Show("Неверный логин или пароль. Проверьте данные и повторите вход.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            ApplyRole("Гость", "Гость");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            TbLogin.Text = "";
            PbPassword.Password = "";
        }

        private void BtnProductsView_Click(object sender, RoutedEventArgs e)
        {
            TbPageTitle.Text = "Товары";
            LbProducts.Visibility = Visibility.Visible;
            DgOrders.Visibility = Visibility.Collapsed;
            ProductToolsPanel.Visibility = (currentRole == "Менеджер" || currentRole == "Администратор") ? Visibility.Visible : Visibility.Collapsed;
            BtnAddProduct.Visibility = currentRole == "Администратор" ? Visibility.Visible : Visibility.Collapsed;
            BtnAddOrder.Visibility = Visibility.Collapsed;
            LoadProducts();
        }

        private void BtnOrdersView_Click(object sender, RoutedEventArgs e)
        {
            if (currentRole != "Менеджер" && currentRole != "Администратор")
            {
                MessageBox.Show("Раздел заказов доступен только менеджеру и администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            TbPageTitle.Text = "Заказы";
            LbProducts.Visibility = Visibility.Collapsed;
            DgOrders.Visibility = Visibility.Visible;
            ProductToolsPanel.Visibility = Visibility.Collapsed;
            BtnAddProduct.Visibility = Visibility.Collapsed;
            BtnAddOrder.Visibility = currentRole == "Администратор" ? Visibility.Visible : Visibility.Collapsed;
            LoadOrders();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyProductFilter();
        }

        private void ApplyProductFilter()
        {
            EnsureProductColumns();
            DataView view = productsTable.DefaultView;
            string filter = "1 = 1";
            if (currentRole == "Менеджер" || currentRole == "Администратор")
            {
                string search = (TbSearch.Text ?? "").Replace("'", "''").Trim();
                if (search.Length > 0)
                {
                    filter += string.Format(" AND (Article LIKE '%{0}%' OR Title LIKE '%{0}%' OR CategoryTitle LIKE '%{0}%' OR Description LIKE '%{0}%' OR ManufacturerTitle LIKE '%{0}%' OR SupplierTitle LIKE '%{0}%' OR UnitTitle LIKE '%{0}%')", search);
                }
                if (CbSupplierFilter.SelectedValue != null && int.TryParse(CbSupplierFilter.SelectedValue.ToString(), out int supplierId) && supplierId > 0)
                {
                    filter += " AND SupplierID = " + supplierId;
                }
                if (CbSort.SelectedItem is ComboBoxItem item)
                {
                    view.Sort = Convert.ToString(item.Tag);
                }
            }
            else
            {
                view.Sort = "";
                filter = "1 = 1";
            }
            view.RowFilter = filter;
            LbProducts.ItemsSource = view;
            TbCount.Text = "Товаров: " + view.Count;
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin()) return;
            editingProductId = null;
            selectedPhotoPath = "Images/picture.png";
            TbProductEditTitle.Text = "Добавление товара";
            TbProductId.Text = "Автоматически";
            TbArticle.Text = "";
            TbTitle.Text = "";
            TbDescription.Text = "";
            TbCost.Text = "0";
            TbQuantity.Text = "0";
            TbDiscount.Text = "0";
            CbCategory.SelectedIndex = 0;
            CbManufacturer.SelectedIndex = 0;
            CbSupplier.SelectedIndex = 0;
            CbUnit.SelectedIndex = 0;
            ImgProductPreview.Source = new BitmapImage(new Uri("Images/picture.png", UriKind.Relative));
            ProductEditPanel.Visibility = Visibility.Visible;
        }

        private void LbProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (currentRole != "Администратор" || LbProducts.SelectedItem == null) return;
            DataRowView row = (DataRowView)LbProducts.SelectedItem;
            editingProductId = ToInt(row["ID"]);
            selectedPhotoPath = Convert.ToString(row["PhotoPath"]);
            TbProductEditTitle.Text = "Редактирование товара";
            TbProductId.Text = Convert.ToString(row["ID"]);
            TbArticle.Text = Convert.ToString(row["Article"]);
            TbTitle.Text = Convert.ToString(row["Title"]);
            TbDescription.Text = Convert.ToString(row["Description"]);
            TbCost.Text = Convert.ToString(row["Cost"]);
            TbQuantity.Text = Convert.ToString(row["Quantity"]);
            TbDiscount.Text = Convert.ToString(row["Discount"]);
            CbCategory.SelectedValue = row["CategoryID"];
            CbManufacturer.SelectedValue = row["ManufacturerID"];
            CbSupplier.SelectedValue = row["SupplierID"];
            CbUnit.SelectedValue = row["UnitID"];
            ImgProductPreview.Source = new BitmapImage(new Uri(File.Exists(selectedPhotoPath) ? selectedPhotoPath : "Images/picture.png", UriKind.RelativeOrAbsolute));
            ProductEditPanel.Visibility = Visibility.Visible;
        }

        private void BtnChooseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
            if (dialog.ShowDialog() == true)
            {
                Directory.CreateDirectory("Images");
                string target = Path.Combine("Images", Guid.NewGuid().ToString("N") + ".png");
                SaveResizedImage(dialog.FileName, target);
                selectedPhotoPath = target;
                ImgProductPreview.Source = new BitmapImage(new Uri(target, UriKind.RelativeOrAbsolute));
            }
        }

        private void SaveResizedImage(string sourcePath, string targetPath)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(sourcePath, UriKind.Absolute);
            image.DecodePixelWidth = 300;
            image.DecodePixelHeight = 200;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (FileStream stream = new FileStream(targetPath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private void BtnSaveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin()) return;
            if (string.IsNullOrWhiteSpace(TbArticle.Text) || string.IsNullOrWhiteSpace(TbTitle.Text))
            {
                MessageBox.Show("Заполните артикул и наименование товара.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!TryDecimal(TbCost.Text, out decimal cost) || cost < 0 || !int.TryParse(TbQuantity.Text, out int qty) || qty < 0 || !int.TryParse(TbDiscount.Text, out int discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Проверьте цену, количество и скидку. Значения не должны быть отрицательными.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string query = editingProductId.HasValue
                ? @"UPDATE dbo.Products SET Article=@Article, Title=@Title, CategoryID=@CategoryID, Description=@Description, ManufacturerID=@ManufacturerID, SupplierID=@SupplierID, Cost=@Cost, UnitID=@UnitID, Quantity=@Quantity, Discount=@Discount, PhotoPath=@PhotoPath WHERE ID=@ID"
                : @"INSERT INTO dbo.Products (Article, Title, CategoryID, Description, ManufacturerID, SupplierID, Cost, UnitID, Quantity, Discount, PhotoPath) VALUES (@Article, @Title, @CategoryID, @Description, @ManufacturerID, @SupplierID, @Cost, @UnitID, @Quantity, @Discount, @PhotoPath)";
            Execute(query, cmd =>
            {
                if (editingProductId.HasValue) cmd.Parameters.AddWithValue("@ID", editingProductId.Value);
                cmd.Parameters.AddWithValue("@Article", TbArticle.Text.Trim());
                cmd.Parameters.AddWithValue("@Title", TbTitle.Text.Trim());
                cmd.Parameters.AddWithValue("@CategoryID", CbCategory.SelectedValue);
                cmd.Parameters.AddWithValue("@Description", TbDescription.Text.Trim());
                cmd.Parameters.AddWithValue("@ManufacturerID", CbManufacturer.SelectedValue);
                cmd.Parameters.AddWithValue("@SupplierID", CbSupplier.SelectedValue);
                cmd.Parameters.AddWithValue("@Cost", cost);
                cmd.Parameters.AddWithValue("@UnitID", CbUnit.SelectedValue);
                cmd.Parameters.AddWithValue("@Quantity", qty);
                cmd.Parameters.AddWithValue("@Discount", discount);
                cmd.Parameters.AddWithValue("@PhotoPath", selectedPhotoPath);
            });
            ProductEditPanel.Visibility = Visibility.Collapsed;
            LoadProducts();
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin() || !editingProductId.HasValue) return;
            object count = Scalar("SELECT COUNT(*) FROM dbo.OrderItems WHERE ProductID=@ID", cmd => cmd.Parameters.AddWithValue("@ID", editingProductId.Value));
            if (Convert.ToInt32(count) > 0)
            {
                MessageBox.Show("Товар присутствует в заказе, удалить его нельзя.", "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить товар?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Execute("DELETE FROM dbo.Products WHERE ID=@ID", cmd => cmd.Parameters.AddWithValue("@ID", editingProductId.Value));
                ProductEditPanel.Visibility = Visibility.Collapsed;
                LoadProducts();
            }
        }

        private void BtnCloseProductEdit_Click(object sender, RoutedEventArgs e)
        {
            ProductEditPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin()) return;
            editingOrderId = null;
            TbOrderEditTitle.Text = "Добавление заказа";
            TbOrderId.Text = "Автоматически";
            TbOrderArticles.Text = "";
            DpOrderDate.SelectedDate = DateTime.Today;
            DpDeliveryDate.SelectedDate = DateTime.Today;
            CbOrderStatus.SelectedIndex = 0;
            CbPickupPoint.SelectedIndex = 0;
            OrderEditPanel.Visibility = Visibility.Visible;
        }

        private void DgOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (currentRole != "Администратор" || DgOrders.SelectedItem == null) return;
            DataRowView row = (DataRowView)DgOrders.SelectedItem;
            editingOrderId = ToInt(row["ID"]);
            TbOrderEditTitle.Text = "Редактирование заказа";
            TbOrderId.Text = Convert.ToString(row["ID"]);
            TbOrderArticles.Text = Convert.ToString(row["Articles"]);
            CbOrderStatus.SelectedValue = row["StatusID"];
            CbPickupPoint.SelectedValue = row["PickupPointID"];
            DpOrderDate.SelectedDate = Convert.ToDateTime(row["OrderDate"]);
            DpDeliveryDate.SelectedDate = Convert.ToDateTime(row["DeliveryDate"]);
            OrderEditPanel.Visibility = Visibility.Visible;
        }

        private void BtnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin()) return;
            if (!DpOrderDate.SelectedDate.HasValue || !DpDeliveryDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите даты заказа и выдачи.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DataTable parsedItems = BuildOrderItemsTable();
            if (parsedItems == null)
            {
                return;
            }
            if (editingOrderId.HasValue)
            {
                Execute("UPDATE dbo.Orders SET OrderDate=@OrderDate, DeliveryDate=@DeliveryDate, PickupPointID=@PickupPointID, StatusID=@StatusID WHERE ID=@ID", cmd =>
                {
                    cmd.Parameters.AddWithValue("@ID", editingOrderId.Value);
                    cmd.Parameters.AddWithValue("@OrderDate", DpOrderDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@DeliveryDate", DpDeliveryDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@PickupPointID", CbPickupPoint.SelectedValue);
                    cmd.Parameters.AddWithValue("@StatusID", CbOrderStatus.SelectedValue);
                });
                SaveOrderItems(editingOrderId.Value, parsedItems);
            }
            else
            {
                object id = Scalar("INSERT INTO dbo.Orders (OrderDate, DeliveryDate, PickupPointID, ClientID, ReceiveCode, StatusID) OUTPUT INSERTED.ID VALUES (@OrderDate, @DeliveryDate, @PickupPointID, NULL, ABS(CHECKSUM(NEWID())) % 900 + 100, @StatusID)", cmd =>
                {
                    cmd.Parameters.AddWithValue("@OrderDate", DpOrderDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@DeliveryDate", DpDeliveryDate.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@PickupPointID", CbPickupPoint.SelectedValue);
                    cmd.Parameters.AddWithValue("@StatusID", CbOrderStatus.SelectedValue);
                });
                SaveOrderItems(Convert.ToInt32(id), parsedItems);
            }
            OrderEditPanel.Visibility = Visibility.Collapsed;
            LoadOrders();
        }

        private DataTable BuildOrderItemsTable()
        {
            string[] parts = (TbOrderArticles.Text ?? "").Split(',');
            if (parts.Length < 2)
            {
                MessageBox.Show("Укажите состав заказа в формате: артикул, количество.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            DataTable parsedItems = new DataTable();
            parsedItems.Columns.Add("ProductID", typeof(int));
            parsedItems.Columns.Add("Quantity", typeof(int));

            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                string article = parts[i].Trim();
                if (!int.TryParse(parts[i + 1].Trim(), out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Количество в составе заказа должно быть положительным числом.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                object productId = Scalar("SELECT ID FROM dbo.Products WHERE Article=@Article", cmd => cmd.Parameters.AddWithValue("@Article", article));
                if (productId == null)
                {
                    MessageBox.Show("Товар с артикулом " + article + " не найден.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                parsedItems.Rows.Add(Convert.ToInt32(productId), quantity);
            }

            return parsedItems;
        }

        private void SaveOrderItems(int orderId, DataTable parsedItems)
        {
            Execute("DELETE FROM dbo.OrderItems WHERE OrderID=@OrderID", cmd => cmd.Parameters.AddWithValue("@OrderID", orderId));

            foreach (DataRow item in parsedItems.Rows)
            {
                Execute("INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity) VALUES (@OrderID, @ProductID, @Quantity)", cmd =>
                {
                    cmd.Parameters.AddWithValue("@OrderID", orderId);
                    cmd.Parameters.AddWithValue("@ProductID", item["ProductID"]);
                    cmd.Parameters.AddWithValue("@Quantity", item["Quantity"]);
                });
            }
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireAdmin() || !editingOrderId.HasValue) return;
            if (MessageBox.Show("Удалить заказ?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Execute("DELETE FROM dbo.OrderItems WHERE OrderID=@ID; DELETE FROM dbo.Orders WHERE ID=@ID;", cmd => cmd.Parameters.AddWithValue("@ID", editingOrderId.Value));
                OrderEditPanel.Visibility = Visibility.Collapsed;
                LoadOrders();
            }
        }

        private void BtnCloseOrderEdit_Click(object sender, RoutedEventArgs e)
        {
            OrderEditPanel.Visibility = Visibility.Collapsed;
        }

        private bool RequireAdmin()
        {
            if (currentRole == "Администратор") return true;
            MessageBox.Show("Действие доступно только администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private bool TryDecimal(string text, out decimal value)
        {
            return decimal.TryParse((text ?? "").Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private int ToInt(object value)
        {
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }
    }
}
