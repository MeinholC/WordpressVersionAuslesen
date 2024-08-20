using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace WinFormsApp1
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e) 
        {
            string connectionString = "Server=192.168.178.201;Database=domainchecker;User ID=root;Password=1td5rugut8;";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = "SELECT domain FROM domains";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string domain = reader.GetString("domain");
                            string version = await GetWordPressVersionAsync(domain);

                            if (version != null)
                            {
                                using (MySqlConnection insertConn = new MySqlConnection(connectionString))
                                {
                                    await insertConn.OpenAsync();
                                    await InsertOrUpdateVersionAsync(insertConn, domain, version);
                                }
                            }
                            else
                            {
                                //Nothing
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}\r\n");
            }
        }

        private async Task<string> GetWordPressVersionAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string html = await client.GetStringAsync("https://" + url);

                    Regex regex = new Regex(@"<meta name=""generator"" content=""WordPress (\d+\.\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase);
                    Match match = regex.Match(html);

                    return match.Success ? match.Groups[1].Value : null;
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        private async Task InsertOrUpdateVersionAsync(MySqlConnection conn, string domain, string version)
        {
            string checkQuery = "SELECT COUNT(*) FROM wp_versionen WHERE domain = @domain";
            MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
            checkCmd.Parameters.AddWithValue("@domain", domain);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            DateTime letzte_pruefung = DateTime.Now;
            if (count > 0)
            {
                string updateQuery = "UPDATE wp_versionen SET version = @version, letzte_pruefung = @letzte_pruefung WHERE domain = @domain";
                MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@domain", domain);
                updateCmd.Parameters.AddWithValue("@version", version);
                updateCmd.Parameters.AddWithValue("@letzte_pruefung", letzte_pruefung);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                string insertQuery = "INSERT INTO wp_versionen (domain, version, letzte_pruefung) VALUES (@domain, @version, @letzte_pruefung)";
                MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@domain", domain);
                insertCmd.Parameters.AddWithValue("@version", version);
                insertCmd.Parameters.AddWithValue("@letzte_pruefung", letzte_pruefung);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}
