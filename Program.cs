using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

/// <summary>
/// Бот с 2 кнопками: 1) перенаправляет человека на другой канал; 2) перенаправляет сообщения в личный чат
/// 

const string TELEGRAM_TOKEN = "";
const long MY_CHAT_ID = 1111111111111;
bool sentToMeMode = false;


var botClient = new TelegramBotClient(TELEGRAM_TOKEN);
using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { }
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    cancellationToken : cts.Token
    );
var me = await botClient.GetMeAsync();
Console.WriteLine($"Начинаем работу с @" + me.Username);
await Task.Delay(int.MaxValue);
cts.Cancel();



async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    #region [ Главное меню ]
    InlineKeyboardMarkup mainMenu = new InlineKeyboardMarkup(new[]
    {
        new[] { InlineKeyboardButton.WithUrl(text: "Перейти на мой тестовый канал", url: "https://t.me/+8wLodZxCGDdmOWEy") },
        new[] { InlineKeyboardButton.WithCallbackData(text: "Оставить пожелания", callbackData: "sendOrder") }
    });
    #endregion
    InlineKeyboardMarkup backMenu = new(new[] { InlineKeyboardButton.WithCallbackData(text: "К главному меню", callbackData: "toBack") });

    if(update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
    {
        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;
        string firstName = update.Message.From.FirstName;
        Console.WriteLine($"Получено сообщение: '{messageText}' в чате {chatId}");
        
        #region [ Первое сообщение ]
        if(messageText == "/start")
        {
            var task = SendPhoto(chatId, cancellationToken);
            task.Wait();

            Message sendMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Привет {firstName}! \n\nЯ бот для взаимодействия с каналом" + Environment.NewLine + "Что ты хочешь сделать?",
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken
                );
        }
        #endregion

        if (sentToMeMode)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Я получил твое сообщение",
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken
                );
            Message sendMessageToMe = await botClient.SendTextMessageAsync(
                chatId: MY_CHAT_ID,
                text: messageText + Environment.NewLine + $"Сщщбщение от @{update.Message.From.Username}",
                cancellationToken: cancellationToken
                );
        }

        if(update.CallbackQuery != null)
        {
            Message sentMessage = await botClient.EditMessageTextAsync(
                messageId: update.CallbackQuery.Message.MessageId,
                chatId: update.CallbackQuery.Message.Chat.Id,
                text: "Напишите тему, на которую хотели бы поговрить" + Environment.NewLine + "Я обязательно это учту",
                replyMarkup: backMenu,
                cancellationToken: cancellationToken
                );
            sentToMeMode = true;
        }
        if(update.CallbackQuery.Data == "toBack")
        {
            Message sentMessage = await botClient.EditMessageTextAsync(
                messageId: update.CallbackQuery.Message.MessageId,
                chatId: update.CallbackQuery.Message.Chat.Id,
                text: "Что ты хочешь?",
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken
                );
            sentToMeMode = false;
        }
    }
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}


async Task SendPhoto(long chatId, CancellationToken token)
{
    Message message = await botClient.SendPhotoAsync(
        chatId: chatId,
        photo: "https://ok.ru/profile/100306518708/pphotos/169508359092",
        parseMode: ParseMode.Html,
        cancellationToken: token
        );
}


