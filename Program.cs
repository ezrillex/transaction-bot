using System;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using TestApp;
using System.Collections.Generic;
using RestSharp;
using System.Text.Json;
using System.Threading;

Console.WriteLine($"{DateTime.Now.ToString()} | BOT STARTING UP ");

string imapUsername = Environment.GetEnvironmentVariable("IMAP_USERNAME")
    ?? throw new InvalidOperationException("Set the IMAP_USERNAME environment variable.");
string imapPassword = Environment.GetEnvironmentVariable("IMAP_PASSWORD")
    ?? throw new InvalidOperationException("Set the IMAP_PASSWORD environment variable.");
string fireflyToken = Environment.GetEnvironmentVariable("FIREFLY_API_TOKEN")
    ?? throw new InvalidOperationException("Set the FIREFLY_API_TOKEN environment variable.");


while (true)
{
    List<Transaction> transactions = new List<Transaction>();

    using (var mail_client = new ImapClient())
    {
        mail_client.Connect("abarca.dev", 993, true);

        // Note: only needed if the SMTP server requires authentication
        mail_client.Authenticate(imapUsername, imapPassword);

        var inbox = mail_client.Inbox;
        inbox.Open(FolderAccess.ReadWrite);
        var messages = inbox.Search(SearchQuery.NotSeen);

        foreach (var msg in messages)
        {
            var message = inbox.GetMessage(msg);

            if (message.Subject.Contains("Alerta PRF BAC"))
            {

                var parsed = message.TextBody.Replace("\n", "").Replace("\r", "|");

                string card = "";

                if (parsed.Contains("*7735*")) card = "BAC Credit Card";
                if (parsed.Contains("*3673*")) card = "BAC Debit Card";

                int im = parsed.IndexOf("*Monto*");
                int id = parsed.IndexOf("*Fecha y hora*");
                int it = parsed.IndexOf("*Tipo de la compra*");

                string detalles = parsed.Substring(im + 7, id - (im + 7));
                string fecha = parsed.Substring(id + 14, it - (id + 14));

                string descripcion = detalles.Split("|", StringSplitOptions.RemoveEmptyEntries)[0];
                string lana = detalles.Split("|", StringSplitOptions.RemoveEmptyEntries)[1];
                fecha = fecha.Replace("|", "");

                var t = new Transaction(descripcion, lana, fecha, card);
                if (t.IsValid())
                {
                    transactions.Add(t);
                    inbox.AddFlags(msg, MessageFlags.Seen, true);
                }
                else
                {
                    Console.WriteLine("ERROR: Transaction is not valid!");
                    Console.WriteLine(t.Info());
                }
            }
        }

        // marks as seen even if message wasn't valid
        inbox.AddFlags(messages, MessageFlags.Seen, true); // this marks everything unread as read even if it did not match with "BAC" subject

        mail_client.Disconnect(true);
    }

    foreach (Transaction transaction in transactions)
    {
        string tjson = JsonSerializer.Serialize(transaction);
        // Console.WriteLine(transaction.Info());
        //  Console.WriteLine(  JsonSerializer.Serialize(transaction) );
        var client = new RestClient("https://money.abarca.dev/");
        var request = new RestRequest("public/api/v1/transactions", method: Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {fireflyToken}");
        request.AddParameter("application/json",
            "{\"transactions\":[ " + tjson + " ]}", ParameterType.RequestBody);
        var response = client.Execute(request);
        Console.WriteLine($"Sent request to firefly. \nResponse Successful: {response.IsSuccessful} \nRequest: {response.Request}");
    }

    // TODO auto clasificar cuenta destino/categoria dado nombre de transaccion. 

    Console.WriteLine($"{DateTime.Now.ToString()} | BOT FINISHED WORK ");

    Thread.Sleep(1000 * 60 * 10);
}

