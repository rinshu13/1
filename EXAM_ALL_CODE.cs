using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Magazin
{
    public partial class MainWindow : Window
    {
        private const string ConnectionString =
            @"Data Source=localhost;Initial Catalog=Magazin;Integrated Security=True";

        private readonly DataTable _itemsTable = new DataTable();
        private readonly DataTable _categoryTable = new DataTable();
        private readonly DataTable _loginTable = new DataTable();

        private DataView _itemsView;
        private int? _editingId;

        public MainWindow()
        {
            InitializeComponent();
            CbSort.SelectedIndex = 0;
            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadLoginData();
            LoadCategories();
            LoadItems();
        }

        private void LoadLoginData()
        {
            _loginTable.Clear();
            FillTable("SELECT ID, Login, [Password], RoleTitle FROM dbo.vLogin", _loginTable);
        }

        private void LoadCategories()
        {
            _categoryTable.Clear();
            FillTable("SELECT ID, Title FROM dbo.Category ORDER BY Title", _categoryTable);

            DataRow allRow = _categoryTable.NewRow();
            allRow["ID"] = 0;
            allRow["Title"] = "Все категории";
            _categoryTable.Rows.InsertAt(allRow, 0);

            CbFilter.ItemsSource = _categoryTable.DefaultView;
            CbFilter.SelectedValue = 0;

            CbEditCategory.ItemsSource = _categoryTable.DefaultView;
            CbEditCategory.SelectedValue = 0;
        }

        private void LoadItems()
        {
            _itemsTable.Clear();
            FillTable(
                @"SELECT
                      ID,
                      Article,
                      Title,
                      CategoryID,
                      Cost,
                      Discount,
                      Quantity,
                      Description,
                      MainImagePath,
                      IsActive
                  FROM dbo.MainObject
                  WHERE IsActive = 1",
                _itemsTable);

            PrepareItemRows();

            _itemsView = _itemsTable.DefaultView;
            DgItems.ItemsSource = _itemsView;
            ApplyFilterSort();
        }

        private void PrepareItemRows()
        {
            AddColumnIfMissing("HasDiscount", typeof(bool));
            AddColumnIfMissing("HighDiscount", typeof(bool));
            AddColumnIfMissing("NoStock", typeof(bool));
            AddColumnIfMissing("FinalCost", typeof(decimal));
            AddColumnIfMissing("DiscountPercentText", typeof(string));

            foreach (DataRow row in _itemsTable.Rows)
            {
                decimal cost = ToDecimal(row["Cost"]);
                double discount = ToDouble(row["Discount"]);
                int quantity = ToInt(row["Quantity"]);

                row["HasDiscount"] = discount > 0;
                row["HighDiscount"] = discount > 0.15;
                row["NoStock"] = quantity <= 0;
                row["FinalCost"] = Math.Round(cost * (decimal)(1 - discount), 2);
                row["DiscountPercentText"] = discount > 0
                    ? "Действующая скидка " + Math.Round(discount * 100) + "%"
                    : "Скидка отсутствует";
            }
        }

        private void AddColumnIfMissing(string columnName, Type type)
        {
            if (!_itemsTable.Columns.Contains(columnName))
            {
                _itemsTable.Columns.Add(columnName, type);
            }
        }

        private void FillTable(string query, DataTable table)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                adapter.Fill(table);
            }
        }

        private void ApplyFilterSort()
        {
            if (_itemsView == null)
            {
                return;
            }

            string search = (TbSearch.Text ?? string.Empty).Replace("'", "''").Trim();
            string filter = "1 = 1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter += string.Format(
                    " AND (Title LIKE '%{0}%' OR Article LIKE '%{0}%' OR Description LIKE '%{0}%')",
                    search);
            }

            if (CbFilter.SelectedValue != null &&
                int.TryParse(CbFilter.SelectedValue.ToString(), out int categoryId) &&
                categoryId > 0)
            {
                filter += " AND CategoryID = " + categoryId;
            }

            _itemsView.RowFilter = filter;

            if (CbSort.SelectedItem is ComboBoxItem item)
            {
                _itemsView.Sort = Convert.ToString(item.Tag);
            }

            TbCount.Text = string.Format("Показано: {0} из {1}", _itemsView.Count, _itemsTable.Rows.Count);
        }

        private DataRowView SelectedItem
        {
            get { return DgItems.SelectedItem as DataRowView; }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string password = PbPassword.Password.Trim();

            foreach (DataRow row in _loginTable.Rows)
            {
                if (Convert.ToString(row["Login"]) == login &&
                    Convert.ToString(row["Password"]) == password)
                {
                    LoginPanel.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            MessageBox.Show("Неверный логин или пароль.");
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterSort();
        }

        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilterSort();
        }

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilterSort();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingId = null;
            TbEditHeader.Text = "Добавление записи";
            ClearEditFields();
            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Выберите запись.");
                return;
            }

            _editingId = Convert.ToInt32(SelectedItem["ID"]);
            TbEditHeader.Text = "Изменение записи";

            TbEditArticle.Text = Convert.ToString(SelectedItem["Article"]);
            TbEditTitle.Text = Convert.ToString(SelectedItem["Title"]);
            CbEditCategory.SelectedValue = SelectedItem["CategoryID"];
            TbEditCost.Text = Convert.ToString(SelectedItem["Cost"]);
            TbEditDiscount.Text = Convert.ToString(SelectedItem["Discount"]);
            TbEditQuantity.Text = Convert.ToString(SelectedItem["Quantity"]);
            TbEditDescription.Text = Convert.ToString(SelectedItem["Description"]);

            EditPanel.Visibility = Visibility.Visible;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Выберите запись.");
                return;
            }

            if (MessageBox.Show(
                    "Удалить выбранную запись?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            int id = Convert.ToInt32(SelectedItem["ID"]);

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand("UPDATE dbo.MainObject SET IsActive = 0 WHERE ID = @ID", connection))
            {
                command.Parameters.AddWithValue("@ID", id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            LoadItems();
        }

        private void BtnSaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbEditTitle.Text))
            {
                MessageBox.Show("Введите название.");
                return;
            }

            if (!TryReadDecimal(TbEditCost.Text, out decimal cost))
            {
                MessageBox.Show("Цена должна быть числом.");
                return;
            }

            if (!TryReadDouble(TbEditDiscount.Text, out double discount))
            {
                MessageBox.Show("Скидка должна быть числом. Например: 0,15");
                return;
            }

            if (!int.TryParse(TbEditQuantity.Text, out int quantity))
            {
                MessageBox.Show("Количество должно быть целым числом.");
                return;
            }

            int? categoryId = null;
            if (CbEditCategory.SelectedValue != null &&
                int.TryParse(CbEditCategory.SelectedValue.ToString(), out int selectedCategoryId) &&
                selectedCategoryId > 0)
            {
                categoryId = selectedCategoryId;
            }

            if (_editingId.HasValue)
            {
                UpdateItem(_editingId.Value, categoryId, cost, discount, quantity);
            }
            else
            {
                InsertItem(categoryId, cost, discount, quantity);
            }

            EditPanel.Visibility = Visibility.Collapsed;
            LoadItems();
        }

        private void InsertItem(int? categoryId, decimal cost, double discount, int quantity)
        {
            const string query =
                @"INSERT INTO dbo.MainObject
                    (Article, Title, CategoryID, Cost, Discount, Quantity, Description, IsActive)
                  VALUES
                    (@Article, @Title, @CategoryID, @Cost, @Discount, @Quantity, @Description, 1)";

            ExecuteSaveCommand(query, null, categoryId, cost, discount, quantity);
        }

        private void UpdateItem(int id, int? categoryId, decimal cost, double discount, int quantity)
        {
            const string query =
                @"UPDATE dbo.MainObject
                  SET Article = @Article,
                      Title = @Title,
                      CategoryID = @CategoryID,
                      Cost = @Cost,
                      Discount = @Discount,
                      Quantity = @Quantity,
                      Description = @Description
                  WHERE ID = @ID";

            ExecuteSaveCommand(query, id, categoryId, cost, discount, quantity);
        }

        private void ExecuteSaveCommand(
            string query,
            int? id,
            int? categoryId,
            decimal cost,
            double discount,
            int quantity)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Article", NullIfEmpty(TbEditArticle.Text));
                command.Parameters.AddWithValue("@Title", TbEditTitle.Text.Trim());
                command.Parameters.AddWithValue("@CategoryID", (object)categoryId ?? DBNull.Value);
                command.Parameters.AddWithValue("@Cost", cost);
                command.Parameters.AddWithValue("@Discount", discount);
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@Description", NullIfEmpty(TbEditDescription.Text));

                if (id.HasValue)
                {
                    command.Parameters.AddWithValue("@ID", id.Value);
                }

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private object NullIfEmpty(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            return value.Trim();
        }

        private bool TryReadDecimal(string text, out decimal value)
        {
            text = (text ?? string.Empty).Replace(',', '.');
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private bool TryReadDouble(string text, out double value)
        {
            text = (text ?? string.Empty).Replace(',', '.');
            return double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private decimal ToDecimal(object value)
        {
            if (value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDecimal(value);
        }

        private double ToDouble(object value)
        {
            if (value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDouble(value);
        }

        private int ToInt(object value)
        {
            if (value == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(value);
        }

        private void ClearEditFields()
        {
            TbEditArticle.Text = string.Empty;
            TbEditTitle.Text = string.Empty;
            CbEditCategory.SelectedValue = 0;
            TbEditCost.Text = "0";
            TbEditDiscount.Text = "0";
            TbEditQuantity.Text = "0";
            TbEditDescription.Text = string.Empty;
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
        }

        private void DgItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BtnEdit_Click(sender, e);
        }

    }
}
