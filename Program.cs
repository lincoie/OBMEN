using System;
using System.IO;
using Renci.SshNet;

class Program
{
    static void Main()
    {
        string host = "192.168.1.11";     
        int port = 22;                     
        string username = "User";          
        string password = "User@ss";      

        string localFile = @"";           // вот сюда пиши откуда отправишь
        string remotePath = "";           // вот сюда пиши куда отпрвишь

        try
        {
            using (var client = new ScpClient(host, port, username, password))
            {
                client.Connect();
                Console.WriteLine("Подключено по SCP");
                using (var fileStream = File.OpenRead(localFile))
                {
                    client.Upload(fileStream, remotePath);
                }

                Console.WriteLine("Файл успешно отправлен: " + remotePath);
                client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }

        Console.WriteLine("Нажми Enter для выхода...");
        Console.ReadLine();
    }
}