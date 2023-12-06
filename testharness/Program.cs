using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Configuration;
using System.Text;

namespace testharness
{
    class Program
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["default"].ConnectionString;
        private static bool exitRequested = false;

        static void Main()
        {
            Console.WriteLine("Chef application is waiting for new orders. Press [Enter] to exit.");
            // Start a background thread to process orders
            Thread orderProcessingThread = new Thread(ProcessOrders);
            orderProcessingThread.Start();

            // Main thread will print "Waiting..." until Enter is pressed
            Console.ReadLine();

            // Signal the background thread to exit
            exitRequested = true;

            // Wait for the background thread to finish
            orderProcessingThread.Join();
        }

        static void ProcessOrders()
        {
            try
            {
                // Fetch configuration parameters
                var pollingInterval = GetConfigurationValue("ChefConsoleAppInterval");
                if (!int.TryParse(pollingInterval, out var interval))
                {
                    interval = 1000; // Default polling interval if configuration is not valid
                }

                while (!exitRequested)
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        using (var command = new SqlCommand("WAITFOR (RECEIVE * FROM OrderNotificationQueue)", connection))
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Process the new order
                                if (reader.IsDBNull(0))
                                {
                                    continue; // Skip null messages
                                }

                                // Retrieve the value as a stream of bytes
                                var orderXmlBytes = (byte[])reader["message_body"];

                                // Convert the byte stream to a string
                                var orderXmlString = Encoding.Unicode.GetString(orderXmlBytes);


                                Console.WriteLine($"New order received: {orderXmlString.ToString()}");

                                // Additional processing logic can be added here
                            }
                        }
                    }

                    // Sleep for the specified interval before checking for new orders again
                    Thread.Sleep(interval);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing orders: {ex.Message}");
            }
        }








        static string GetConfigurationValue(string key)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT Value FROM Configuration WHERE [Key] = @Key", connection))
                {
                    command.Parameters.AddWithValue("@Key", key);
                    return (string)command.ExecuteScalar();
                }
            }
        }

        static List<MenuItem> GetMenuItems()
        {
            List<MenuItem> menuItems = new List<MenuItem>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT * FROM [dbo].[Menu]", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        menuItems.Add(new MenuItem
                        {
                            ItemId = reader.GetInt32(reader.GetOrdinal("ItemId")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                            Category = reader.GetString(reader.GetOrdinal("Category"))
                        });
                    }
                }
            }

            return menuItems;
        }

        static List<Order> GetNewOrders()
        {
            List<Order> orders = new List<Order>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Fetch new orders from the RestaurantOrder table
                using (var command = new SqlCommand("SELECT * FROM [dbo].[RestaurantOrder]", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new Order
                        {
                            OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                            Item = reader.GetString(reader.GetOrdinal("Item")),
                            Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                        });
                    }
                }
            }

            return orders;
        }
    }

    class MenuItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    class Order
    {
        public int OrderId { get; set; }
        public string Item { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
