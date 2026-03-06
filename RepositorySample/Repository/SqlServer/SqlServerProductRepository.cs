using Microsoft.Data.SqlClient;
using RepositorySample.Entities;
using System.Data;
using System.Text;

namespace RepositorySample.Repository.SqlServer
{
    internal class SqlServerProductRepository : IProductRepository
    {
        private const string INSERT_COMMAND = "INSERT INTO Products VALUES (@ProductId, @ProductName, @ProductPrice, @Quantity)";
        private const string UPDATE_COMMAND = "UPDATE Products SET ProductName = @ProductName, ProductPrice = @ProductPrice, Quantity = @Quantity WHERE ProductId = @ProductId";
        private const string FIND_BY_ID_QUERY = "SELECT ProductName, ProductPrice, Quantity FROM Products WHERE ProductId = @ProductId";
        private const string SELECT = "SELECT ";
        private const string FIND_ALL = "ProductId, ProductName, ProductPrice, Quantity FROM Products WHERE (1 = 1)";
        private const string DELETE_ALL = "DELETE FROM Products";

        private readonly SqlConnection connection;
        private readonly SqlTransaction? transaction;

        public SqlServerProductRepository(SqlConnection connection, SqlTransaction? transaction)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.transaction = transaction;
        }

        public Product? Add(Product product)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = INSERT_COMMAND;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.Parameters.Add(new SqlParameter("@ProductId", System.Data.SqlDbType.UniqueIdentifier)).Value = product.Id;
            cmd.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 255)).Value = product.Name;
            cmd.Parameters.Add(new SqlParameter("@ProductPrice", System.Data.SqlDbType.Float)).Value = product.Price;
            cmd.Parameters.Add(new SqlParameter("@Quantity", System.Data.SqlDbType.Int)).Value = product.Quantity;

            if (cmd.ExecuteNonQuery() > 0)
            {
                return product;
            }
            else
            {
                return null;
            }
        }

        public int DeleteAll()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = DELETE_ALL;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            return cmd.ExecuteNonQuery();
        }

        public IEnumerable<Product> Find(ProductFindCreterias creterias, ProductSortBy sortBy = ProductSortBy.NameAscending)
        {
            var cmd = connection.CreateCommand();
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            var sql = new StringBuilder(SELECT);
            if (creterias.Take > 0)
            {
                sql.Append("TOP ");
                sql.Append(creterias.Take);
                sql.Append(' ');
            }
            sql.Append(FIND_ALL);

            if (creterias.Ids.Any())
            {
                sql.Append(" AND ProductId IN (");
                sql.Append(string.Join(',', creterias.Ids.Select(id => $"'{id}'")));
                sql.Append(')');
            }

            if (creterias.MinPrice != double.MinValue)
            {
                sql.Append(" AND ProductPrice >= @MinPrice");
                cmd.Parameters.Add(new SqlParameter("@MinPrice", SqlDbType.Float)).Value = creterias.MinPrice;
            }

            if (creterias.MaxPrice != double.MaxValue)
            {
                sql.Append(" AND ProductPrice <= @MaxPrice");
                cmd.Parameters.Add(new SqlParameter("@MaxPrice", SqlDbType.Float)).Value = creterias.MaxPrice;
            }

            if (!string.IsNullOrEmpty(creterias.Name))
            {
                sql.Append(" AND ProductName LIKE @ProductName");
                cmd.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 255)).Value = "%" + creterias.Name + "%";
            }

            if (sortBy == ProductSortBy.PriceAscending)
            {
                sql.Append(" ORDER BY ProductPrice");
            }
            else if (sortBy == ProductSortBy.PriceDescending)
            {
                sql.Append(" ORDER BY ProductPrice DESC");
            }
            else if (sortBy == ProductSortBy.NameDescending)
            {
                sql.Append(" ORDER BY ProductName");
            }
            else 
            {
                sql.Append(" ORDER BY ProductName DESC");
            }

            if (creterias.Skip > 0)
            {
                sql.Append(" OFFSET ");
                sql.Append(creterias.Skip);
                sql.Append(" ROWS");
            }

            cmd.CommandText = sql.ToString();
            using var reader = cmd.ExecuteReader();
            var products = new List<Product>();

            if (reader != null)
            {
                while (reader.Read())
                {
                    products.Add(new Product()
                    {
                        Id = reader.GetGuid(0),
                        Name = reader.GetString(1),
                        Price = reader.GetDouble(2),
                        Quantity = reader.GetInt32(3),
                    });
                }
            }

            return products;
        }

        public Product? FindById(Guid id)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = FIND_BY_ID_QUERY;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.Parameters.Add(new SqlParameter("@ProductId", System.Data.SqlDbType.UniqueIdentifier)).Value = id;

            using var reader = cmd.ExecuteReader();
            if (reader != null && reader.Read())
            {
                return new Product()
                {
                    Id = id,
                    Name = reader.GetString(0),
                    Price = reader.GetDouble(1),
                    Quantity = reader.GetInt32(2),
                };
            }
            else
            {
                return null;
            }
        }

        public int Update(Product product)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = UPDATE_COMMAND;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            cmd.Parameters.Add(new SqlParameter("@ProductId", System.Data.SqlDbType.UniqueIdentifier)).Value = product.Id;
            cmd.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 255)).Value = product.Name;
            cmd.Parameters.Add(new SqlParameter("@ProductPrice", System.Data.SqlDbType.Float)).Value = product.Price;
            cmd.Parameters.Add(new SqlParameter("@Quantity", System.Data.SqlDbType.Int)).Value = product.Quantity;

            return cmd.ExecuteNonQuery();
        }
    }
}
