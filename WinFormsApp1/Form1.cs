using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Security.Policy;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
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

                    string query = "SELECT webseite FROM webCrawler";
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string domain = reader.GetString("webseite");
                            //string version = await GetWordPressVersionAsync(domain);
                            string versionInfo = await GetCmsVersionAsync(domain); // <-- Methodenaufruf geändert

                            if (versionInfo != null)
                            {
                                string[] parts = versionInfo.Split('|'); // <-- Aufteilung in CMS-Namen und Version
                                string cmsName = parts[0];
                                string version = parts[1];

                                using (MySqlConnection insertConn = new MySqlConnection(connectionString))
                                {
                                    await insertConn.OpenAsync();
                                    await InsertOrUpdateVersionAsync(insertConn, domain, version, cmsName);
                                }
                            }
                            else
                            {
                                //Nothing
                            }
                        }
                        Console.Beep();
                        MessageBox.Show("Fertig");
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

        private async Task InsertOrUpdateVersionAsync(MySqlConnection conn, string domain, string version, string cmsName)
        {
            string checkQuery = "SELECT COUNT(*) FROM wp_versionen WHERE domain = @domain";
            MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
            checkCmd.Parameters.AddWithValue("@domain", domain);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            DateTime letzte_pruefung = DateTime.Now;
            if (count > 0)
            {
                string updateQuery = "UPDATE wp_versionen SET version = @version, letzte_pruefung = @letzte_pruefung , cms_name=@cmsname WHERE domain = @domain";
                MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@domain", domain);
                updateCmd.Parameters.AddWithValue("@version", version);
                updateCmd.Parameters.AddWithValue("@letzte_pruefung", letzte_pruefung);
                updateCmd.Parameters.AddWithValue("@cmsname", cmsName);
                await updateCmd.ExecuteNonQueryAsync();
            }
            else
            {
                string insertQuery = "INSERT INTO wp_versionen (domain, version, letzte_pruefung, cms_name) VALUES (@domain, @version, @letzte_pruefung, @cmsname)";
                MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@domain", domain);
                insertCmd.Parameters.AddWithValue("@version", version);
                insertCmd.Parameters.AddWithValue("@letzte_pruefung", letzte_pruefung);
                insertCmd.Parameters.AddWithValue("@cmsname", cmsName);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
        private async Task<string> GetCmsVersionAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string html = await client.GetStringAsync("https://" + url);

                    // WordPress
                    Regex wpRegex = new Regex(@"<meta name=""generator"" content=""WordPress (\d+\.\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase);
                    Match wpMatch = wpRegex.Match(html);
                    if (wpMatch.Success)
                    {
                        return $"WordPress|{wpMatch.Groups[1].Value}";
                    }

                    // Joomla
                    Regex joomlaRegex = new Regex(@"<meta name=""generator"" content=""Joomla\! - Open Source Content Management (\d+\.\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase);
                    Match joomlaMatch = joomlaRegex.Match(html);
                    if (joomlaMatch.Success)
                    {
                        return $"Joomla|{joomlaMatch.Groups[1].Value}";
                    }

                    // Alternative Joomla-Erkennung: Prüfen auf typische Joomla-Dateien
                    if (html.Contains("com_content"))
                    {
                        return "Joomla|Unknown";
                    }

                    // Drupal
                    Regex drupalRegex = new Regex(@"<meta name=""generator"" content=""Drupal (\d+\.\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase);
                    Match drupalMatch = drupalRegex.Match(html);
                    if (drupalMatch.Success)
                    {
                        return $"Drupal|{drupalMatch.Groups[1].Value}";
                    }

                    // Alternative Drupal-Erkennung: Prüfen auf typische URLs
                    if (html.Contains("/sites/default/files/"))
                    {
                        return "Drupal|Unknown";
                    }

                    // Typo3
                    Regex typo3Regex = new Regex(@"<meta name=""generator"" content=""TYPO3 CMS (\d+\.\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase);
                    Match typo3Match = typo3Regex.Match(html);
                    if (typo3Match.Success)
                    {
                        return $"Typo3|{typo3Match.Groups[1].Value}";
                    }

                    // Alternative Typo3-Erkennung: Prüfen auf typische URLs
                    if (html.Contains("typo3conf"))
                    {
                        return "Typo3|Unknown";
                    }

                    // Weitere CMS-Erkennung hinzufügen...

                    return null; // Kein CMS erkannt
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
    }
}

