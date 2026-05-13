using UnityEngine;
using MySql.Data.MySqlClient;
using System;

public static class MySQLHelper
{    private static string connectionString = "Server=localhost;Port=3306;Database=game_stats;Uid=admin;Pwd=1111;SslMode=none;AllowPublicKeyRetrieval=True;";

    public static void SaveMatchResult(string winner, int money, int artifacts, float duration)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                string query = "INSERT INTO match_results (winner_team, thieves_money, artifacts_deposited, match_duration_sec) VALUES (@winner, @money, @artifacts, @duration)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@winner", winner);
                    cmd.Parameters.AddWithValue("@money", money);
                    cmd.Parameters.AddWithValue("@artifacts", artifacts);
                    cmd.Parameters.AddWithValue("@duration", duration);
                    cmd.ExecuteNonQuery();
                }
                Debug.Log("Результат сохранён в MySQL!");
            }
            catch (Exception e)
            {
                Debug.LogError("Ошибка сохранения результата: " + e.Message);
            }
        }
    }
}