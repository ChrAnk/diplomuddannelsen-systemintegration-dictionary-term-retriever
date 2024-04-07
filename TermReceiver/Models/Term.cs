using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TermReceiver.Models
{
    public class Term
    {
        public string? title { get; set; }
        public string? plural { get; set; }
        public string? @class { get; set; }
        public string? gender { get; set; }

        public void SaveTerm()
        {
            var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            string connectionString = $"Data Source=localhost;Initial Catalog=TermDatabase;Persist Security Info=True;User ID={secrets["SqlServerUsername"]};Password={secrets["SqlServerPassword"]};TrustServerCertificate=True";

            SqlConnection connection = new SqlConnection(connectionString);

            connection.Open();

            string query = $"INSERT INTO Terms (Title, Plural, Class, Gender) VALUES ('{title}', '{plural}', '{@class}', '{gender}');";

            SqlCommand command = new SqlCommand(query, connection);

            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}
