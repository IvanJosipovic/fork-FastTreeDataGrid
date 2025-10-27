using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace FastTreeDataGrid.Demo.ViewModels.Crud;

public sealed class SqliteCrudService
{
    private readonly string _connectionString;
    private readonly Task _initializationTask;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SqliteCrudService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "FastTreeDataGrid.Demo");
        Directory.CreateDirectory(directory);

        var databasePath = Path.Combine(directory, "crud-sample.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };

        _connectionString = builder.ToString();
        _initializationTask = InitializeAsync();
    }

    public Task EnsureInitializedAsync() => _initializationTask;

    public async Task<IReadOnlyList<SqliteCategoryRecord>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        var result = new List<SqliteCategoryRecord>();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name
            FROM Categories
            ORDER BY Name COLLATE NOCASE;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            result.Add(new SqliteCategoryRecord(id, name));
        }

        return result;
    }

    public async Task<IReadOnlyList<SqliteProductRecord>> GetProductsAsync(CancellationToken cancellationToken)
    {
        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        var result = new List<SqliteProductRecord>();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CategoryId, Name, Price
            FROM Products
            ORDER BY Name COLLATE NOCASE;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetInt32(0);
            var categoryId = reader.GetInt32(1);
            var name = reader.GetString(2);
            var price = reader.GetDecimal(3);
            result.Add(new SqliteProductRecord(id, categoryId, name, price));
        }

        return result;
    }

    public async Task<int> CreateCategoryAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Categories(Name)
            VALUES ($name);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", name.Trim());

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task<int> CreateProductAsync(int categoryId, string name, decimal price, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Products(CategoryId, Name, Price)
            VALUES ($categoryId, $name, $price);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$price", price);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task UpdateCategoryAsync(int id, string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name cannot be empty.", nameof(name));
        }

        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE Categories
            SET Name = $name
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateProductAsync(int id, int categoryId, string name, decimal price, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE Products
            SET Name = $name,
                Price = $price,
                CategoryId = $categoryId
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$price", price);
        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM Categories
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        await _initializationTask.ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM Products
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InitializeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await EnableForeignKeysAsync(connection, CancellationToken.None).ConfigureAwait(false);

            await using (var categories = connection.CreateCommand())
            {
                categories.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS Categories(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL COLLATE NOCASE
                    );
                    """;
                await categories.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await using (var products = connection.CreateCommand())
            {
                products.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS Products(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CategoryId INTEGER NOT NULL,
                        Name TEXT NOT NULL COLLATE NOCASE,
                        Price REAL NOT NULL,
                        FOREIGN KEY(CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
                    );
                    """;
                await products.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await using (var seedCheck = connection.CreateCommand())
            {
                seedCheck.CommandText = "SELECT COUNT(*) FROM Categories;";
                var count = Convert.ToInt32(await seedCheck.ExecuteScalarAsync().ConfigureAwait(false), CultureInfo.InvariantCulture);
                if (count > 0)
                {
                    return;
                }
            }

            using var transaction = connection.BeginTransaction();

            var seedCategories = new[]
            {
                ("Cloud Services", new[] { ("Managed Kubernetes", 189.00m), ("Object Storage", 39.00m), ("Edge CDN", 49.00m) }),
                ("Productivity", new[] { ("Team Collaboration Suite", 24.99m), ("Automation Toolkit", 59.00m), ("Analytics Workspace", 89.00m) }),
                ("Devices", new[] { ("Convertible Laptop", 1299.00m), ("Noise-Cancelling Headphones", 299.00m), ("4K Conference Camera", 499.00m) }),
            };

            await using var insertCategory = connection.CreateCommand();
            insertCategory.Transaction = transaction;
            insertCategory.CommandText =
                """
                INSERT INTO Categories(Name)
                VALUES ($name);
                """;
            var categoryName = insertCategory.Parameters.Add("$name", SqliteType.Text);

            await using var insertProduct = connection.CreateCommand();
            insertProduct.Transaction = transaction;
            insertProduct.CommandText =
                """
                INSERT INTO Products(CategoryId, Name, Price)
                VALUES ($categoryId, $name, $price);
                """;
            var productCategory = insertProduct.Parameters.Add("$categoryId", SqliteType.Integer);
            var productName = insertProduct.Parameters.Add("$name", SqliteType.Text);
            var productPrice = insertProduct.Parameters.Add("$price", SqliteType.Real);

            foreach (var (category, products) in seedCategories)
            {
                categoryName.Value = category;
                await insertCategory.ExecuteNonQueryAsync().ConfigureAwait(false);

                await using var lastIdCommand = connection.CreateCommand();
                lastIdCommand.Transaction = transaction;
                lastIdCommand.CommandText = "SELECT last_insert_rowid();";
                var categoryId = Convert.ToInt32(await lastIdCommand.ExecuteScalarAsync().ConfigureAwait(false), CultureInfo.InvariantCulture);

                foreach (var (productNameValue, price) in products)
                {
                    productCategory.Value = categoryId;
                    productName.Value = productNameValue;
                    productPrice.Value = price;
                    await insertProduct.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            transaction.Commit();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task EnableForeignKeysAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        await pragma.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

public readonly record struct SqliteCategoryRecord(int Id, string Name);

public readonly record struct SqliteProductRecord(int Id, int CategoryId, string Name, decimal Price);
