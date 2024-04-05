using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DBProjectMaui
{
    public partial class MainPage : ContentPage
    {
        private static SqlConnection connection = new SqlConnection("Server=localhost;Database=master;User Id=SA;Password=Password123");
        public List<string> Parents { get; set; } = new List<string>();
        private static string ParentName = "Projects";
        private static string childTableName = new string("");
        private static string constraintName = new string("");
        private static DataSet dataSetParent = new DataSet();
        private static DataSet dataSetChild = new DataSet();

        public MainPage()
        {
            InitializeComponent();
            // Connect to the SQL database and retrieve data
     
            ConnectAndRetrieveData();
        }
        private async void ConnectAndRetrieveData()
        {
            try
            {
                // Retrieve and display data from each table
                await DisplayTableData(Parents, ParentName);
                // Get the foreign key constraints for the parent table
                GetForeignKeyConstraints(ParentName);
            }
            catch (Exception ex)
            {
                // Handle any errors
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
        public async void GetForeignKeyConstraints(string parentTableName)
        {
            try
            {
                await connection.OpenAsync();
                // Perform database operations here get the schema of the child table
                string query = @"
                    SELECT 
                        OBJECT_NAME(f.parent_object_id) TableName,
                        COL_NAME(fc.parent_object_id,fc.parent_column_id) ColName
                    FROM 
                        sys.foreign_keys AS f
                    INNER JOIN 
                        sys.foreign_key_columns AS fc 
                            ON f.OBJECT_ID = fc.constraint_object_id
                    INNER JOIN 
                        sys.tables t 
                            ON t.OBJECT_ID = fc.referenced_object_id
                    WHERE 
                        OBJECT_NAME (f.referenced_object_id) = @ParentTableName";

                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@ParentTableName", parentTableName);

                DataSet dataSet = new DataSet();
                adapter.Fill(dataSet);

                // Iterate through the result set
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    #pragma warning disable CS8601
                    childTableName = row["TableName"].ToString();
                    constraintName = row["ColName"].ToString();
                    #pragma warning restore CS8601

                    Console.WriteLine($"Child table: {childTableName}, Constraint name: {constraintName}");
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        private async Task DisplayTableData( List<string> dataList, string tableName)
        {
            try
            {
                string query = $"SELECT * FROM {tableName}";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);

                await Task.Run(() => adapter.Fill(dataSetParent, tableName));

                // Iterate through each table in the DataSet
                foreach (DataTable table in dataSetParent.Tables)
                {
                    // Iterate through each row in the table
                    foreach (DataRow row in table.Rows)
                    {
                        string rowData = string.Empty;
                        // Iterate through each column in the row
                        foreach (DataColumn column in table.Columns)
                        {
                            // Add the row data to the list
                            rowData += row[column].ToString() + "~";
                        }
                        dataList.Add(rowData);
                    }
                }
                ParentListView.ItemsSource = dataList;
            }
            catch (Exception ex)
            {
                // Handle any errors
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private async void ParentListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                // Get the selected parent ID
                var parentID = e.SelectedItem?.ToString()?.Split('~')[0];

                try
                {
                    if (!string.IsNullOrEmpty(childTableName))
                    {
                        // Query child entities based on foreign key constraint and parent ID
                        string query = $"SELECT * FROM {childTableName} WHERE {constraintName} = @ParentID";
                        SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                        adapter.SelectCommand.Parameters.AddWithValue("@ParentID", parentID);

                        dataSetChild = new DataSet();
                        await Task.Run(() => adapter.Fill(dataSetChild, childTableName));



                        List<string> childEntities = new List<string>();

                        // Iterate through the "Child" table in the DataSet
                        foreach (DataTable table in dataSetChild.Tables)
                        {
                            // Iterate through each row in the table
                            foreach (DataRow row in table.Rows)
                            {
                                string rowData = string.Empty;
                                // Iterate through each column in the row
                                foreach (DataColumn column in table.Columns)
                                {
                                    // Add the row data to the list
                                    rowData += row[column].ToString() + "~";
                                }
                                childEntities.Add(rowData);
                            }
                        }

                        // Bind the data list to the UI
                        ChildForParentListView.ItemsSource = childEntities;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }
        private async void UpdateChild(string ChildData){
            List<string> ChildParser = [.. ChildData.Split('~')];
            try
            {
                await connection.OpenAsync();
                //Check whatever is the id constraint because it can be EmployeeID or something else
                //constraintName is not my id is the foreign keu

                // Retrieve the column names dynamically from the database schema
                List<string> columnNames = GetColumnNames(childTableName, connection);

                // Prompt the user for input for each column
                Dictionary<string, string> attributeValues = new Dictionary<string, string>();
                foreach (int i in Enumerable.Range(0, columnNames.Count))
                {
                    var columnValue = await DisplayPromptAsync($"Enter value for {columnNames[i]}", $"Enter value for {columnNames[i]}:", initialValue: ChildParser[i]);
                    if (string.IsNullOrWhiteSpace(columnValue))
                    {
                        await DisplayAlert("Error", $"Please enter a valid value for {columnNames[i]}.", "OK");
                        return;
                    }

                    // Add column name and value to the dictionary
                    attributeValues.Add(columnNames[i], columnValue);
                }

                // Construct the UPDATE SQL command dynamically
                string newAttributeValues = string.Join(",", attributeValues.Values.Select(value => $"'{value}'"));

                // Execute the UPDATE SQL command
                string query = GenerateUpdateQuery(attributeValues.Values.ToList(), ChildParser, connection);
                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    await adapter.SelectCommand.ExecuteNonQueryAsync();

                    Console.WriteLine("Child updated successfully.");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"{ex.Message.Split('.')[1].Split('\'')[0].Split('\"')[0]}.", "OK");
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                // Handle any errors
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void DeleteChild(string ChildData)
        {
            List<string> ChildParser = [.. ChildData.Split('~')];
            
            try
            {
                await connection.OpenAsync();
                string query = GenerateDeleteQuery(ChildParser, childTableName, connection);
                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    await adapter.SelectCommand.ExecuteNonQueryAsync();


                    Console.WriteLine("Child deleted successfully.");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"{ex.Message.Split('.')[1].Split('\'')[0].Split('\"')[0]}.", "OK");
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                // Handle any errors
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        static string GenerateUpdateQuery(List<string> newAttributeValue, List<string> attributeValues, SqlConnection connection)
        {
            string query = $"UPDATE {childTableName} SET ";
            List<string> setClauses = new List<string>();

            // Retrieve the column names dynamically from the database schema
            List<string> columnNames = GetColumnNames(childTableName, connection);

            Console.WriteLine("Column names: " + string.Join(", ", columnNames));
            Console.WriteLine("Attribute values: " + string.Join(", ", attributeValues));

            // Generate set clauses for each attribute value
            foreach (int i in Enumerable.Range(0, columnNames.Count))
            {
                setClauses.Add($"{columnNames[i]} = '{newAttributeValue[i]}'");
            }

            query += string.Join(", ", setClauses);
            query += $" WHERE ";
            foreach (int i in Enumerable.Range(0, columnNames.Count))
            {
                query += $"{columnNames[i]} = '{attributeValues[i]}'";
                if (i != columnNames.Count - 1)
                {
                    query += " AND ";
                }
            }
            return query;
        }
        static string GenerateDeleteQuery(List<string> attributeValues, string tableName, SqlConnection connection)
        {
            string query = $"DELETE FROM {tableName} WHERE ";
            List<string> conditions = new List<string>();

            // Retrieve the column names dynamically from the database schema
            List<string> columnNames = GetColumnNames(tableName, connection);

            Console.WriteLine("Column names: " + string.Join(", ", columnNames));
            Console.WriteLine("Attribute values: " + string.Join(", ", attributeValues));

            // Generate conditions for each attribute value
            foreach (int i in Enumerable.Range(0, columnNames.Count))
            {
                conditions.Add($"{columnNames[i]} = '{attributeValues[i]}'");
            }

            query += string.Join(" AND ", conditions);
            return query;
        }
        static List<string> GetColumnNames(string tableName, SqlConnection connection)
        {
            List<string> columnNames = new List<string>();

            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
            SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
            adapter.SelectCommand.Parameters.AddWithValue("@TableName", tableName);

            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet);

            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                #pragma warning disable CS8604
                columnNames.Add(row[0].ToString());
                #pragma warning restore CS8604
            }

            return columnNames;
        }
        private async void DeleteButton_Clicked(object sender, EventArgs e)
        {
            // Get the Child data from the command parameter
            var ChildData = (sender as Button)?.CommandParameter?.ToString();

            // Extract the Child ID or other necessary data
            // Assuming the Child ID is the first part of the string

            // Confirm with the user before deleting
            var confirmation = await DisplayAlert("Confirm Deletion", "Are you sure you want to delete this Child?", "Yes", "No");

            if (confirmation)
            {
                // Call the delete method
                #pragma warning disable CS8604 // Possible null reference argument.
                DeleteChild(ChildData);
                #pragma warning restore CS8604 // Possible null reference argument.
            }
        }

        private void UpdateButton_Clicked(object sender, EventArgs e)
        {
            // Get the Child data from the command parameter
            var ChildData = (sender as Button)?.CommandParameter?.ToString();

            // Extract the Child ID or other necessary data
            // Assuming the Child ID is the first part of the string

            // Call the update method
            #pragma warning disable CS8604 // Possible null reference argument.
            UpdateChild(ChildData);
            #pragma warning restore CS8604 // Possible null reference argument.
        }

        //Problem when the id is overridden some bugs
        private async void InsertButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Create a dictionary to store attribute name-value pairs
                Dictionary<string, string> attributeValues = new Dictionary<string, string>();

                // Iterate through each column in the child table
                await connection.OpenAsync();

                // Retrieve column names dynamically from the child table schema
                string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
                //adaptor
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@TableName", childTableName);
                using (SqlDataReader reader = adapter.SelectCommand.ExecuteReader())
                {
                    while (await reader.ReadAsync())
                    {
                        string columnName = reader.GetString(0);

                        // Prompt the user for input for each column
                        var columnValue = await DisplayPromptAsync($"Enter value for {columnName}", $"Enter value for {columnName}:");
                        if (string.IsNullOrWhiteSpace(columnValue))
                        {
                            await DisplayAlert("Error", $"Please enter a valid value for {columnName}.", "OK");
                            return;
                        }

                        // Add column name and value to the dictionary
                        attributeValues.Add(columnName, columnValue);
                    }
                    connection.Close();
                }

                // Construct the INSERT SQL command dynamically
                string columns = string.Join(",", attributeValues.Keys);
                string values = string.Join(",", attributeValues.Values.Select(value => $"'{value}'"));

                // Execute the INSERT SQL command
                await connection.OpenAsync();

                query = $"INSERT INTO {childTableName} ({columns}) VALUES ({values})";
                try{
                    adapter = new SqlDataAdapter(query, connection);
                    int rowsAffected = await adapter.SelectCommand.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("New row inserted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert new row.");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"{ex.Message.Split('.')[1].Split('\'')[0].Split('\"')[0]}.", "OK");
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                // Handle any errors
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


    }
}
