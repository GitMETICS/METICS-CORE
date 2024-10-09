using MimeKit;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body, byte[] attachmentData = null, string fileName = null)
    {
        bool isEnabled = _configuration.GetValue<bool>("EmailSettings:IsEnabled");

        if (!isEnabled) return;

        var message = new MimeMessage();

        MailboxAddress from = new MailboxAddress(_configuration["EmailSettings:Username"], _configuration["EmailSettings:From"]);

        message.From.Add(from);
        message.To.Add(new MailboxAddress("Receiver", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };

        if (attachmentData != null && fileName != null)
        {
            var attachment = new MimePart("application", "octet-stream")
            {
                Content = new MimeContent(new MemoryStream(attachmentData)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = fileName
            };

            var multipart = new Multipart("mixed");
            multipart.Add(bodyBuilder.ToMessageBody());
            multipart.Add(attachment);
            message.Body = multipart;
        }
        else
        {
            message.Body = bodyBuilder.ToMessageBody();
        }

        using (var client = new MailKit.Net.Smtp.SmtpClient())
        {
            // Configurar el cliente SMTP para el servidor de correo de la UCR
            client.Connect("smtp.ucr.ac.cr", 587); // Se utiliza el puerto 587 para enviar correos
            client.Authenticate(from.Address, _configuration["EmailSettings:Password"]);

            client.Send(message);

            client.Disconnect(true);
        }
    }
}
