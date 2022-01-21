using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Project.Models;

namespace Project
{
    public class ThisBot
    {
        private TelegramBotClient botClient = new TelegramBotClient("5053251455:AAH71IsfaqbCHe_aL41mQkT5vDc0XgvaMuE");
        private CancellationTokenSource cts = new ();
        private BlockingCollection<Update> updates = new ();
       
        public async void Start()
        {
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken: cts.Token);

            var me = await botClient.GetMeAsync();
            CheckTime();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                // Only process Message updates: https://core.telegram.org/bots/api#message
                //message = await botClient.SendTextMessageAsync(chatId: update.Message.Chat.Id, text: $"{update.Message.Sticker.FileId}");
                if (update.Type != UpdateType.Message)
                    return;
                // Only process text messages
                if (update.Message!.Type != MessageType.Text)
                    return;

                var chatId = update.Message.Chat.Id;
                var userMessage = update.Message.Text.ToLower();
                var user = update.Message.From.Username;
                
                Console.WriteLine($"Received a '{userMessage}' message in chat {chatId}.");

               switch (userMessage)
                {
                    case "/start":
                        ConnectDB.InsertChat(chatId, user);
                        ConnectDB.UpdateUserStage(chatId, update.Message.From.Username, "главное меню");
                        await OpenMainMenu(chatId, KeyboardMakups.MainMenu);
                        break;

                    case "главное меню":
                        ConnectDB.UpdateUserStage(chatId, update.Message.From.Username, "главное меню");
                        ConnectDB.DeleteStatisticStage(chatId);
                        await OpenMainMenu(chatId, KeyboardMakups.MainMenu);
                        break;

                    case "добавить задачу":
                        ConnectDB.UpdateUserStage(chatId, user, userMessage);
                        await botClient.SendTextMessageAsync(chatId: chatId, text: "Я весь во внимании", replyMarkup: KeyboardMakups.ExitMenu);
                        await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDFWHobjkXu1-dfswryB4o2VU9eEnqAAMVAAIqVRgCygn9Ylfrq9QjBA");
                        break;

                    case "удалить задачу":
                        ConnectDB.UpdateUserStage(chatId, user, userMessage);
                        await OpenedTask(chatId, true);       
                        break;
                    case "открытые задачи":
                        await OpenedTask(chatId, false);
                        break;
                    case "статистика":
                        ConnectDB.UpdateUserStage(chatId, user, "статистика");
                        ConnectDB.UpdateStatisticStage(chatId, user, "получение начальной даты");
                        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Укажите начальную дату для выгрузки в формате дд.мм.гггг", replyMarkup: KeyboardMakups.ExitMenu);
                        break; 

                    default:
                        var stage = ConnectDB.GetUserStage(chatId);
                        var rkm = KeyboardMakups.ExitMenu;
                        switch(stage)
                        {
                            case "добавить задачу":
                                await RecordUserTasksAsync(chatId, userMessage, user, rkm);
/*                                await Regulator(userMessage, chatId, user, stage, KeyboardMakups.ExitMenu,
                                    async (chatId, userMessage, rkm) =>  // не видит KeyboardMakups.ExitMenu
                                    await RecordUserTasksAsync(chatId, userMessage, user, rkm));*/
                                break;
                            case "удалить задачу":
                                await DeleteUserTasksAsync(chatId, userMessage, rkm);
                                /*await Regulator(userMessage, chatId, user, stage, KeyboardMakups.ExitMenu,
                                    async (chatId, userMessage, rkm) => 
                                    await DeleteUserTasksAsync(chatId, userMessage, rkm));*/
                                break;
                            case "получение прогресса":
                                await GetProgress(chatId, userMessage, rkm);
                                /*await Regulator(userMessage, chatId, user, stage, KeyboardMakups.ExitMenu,
                                    async (chatId, userMessage, rkm) =>
                                    await GetProgress(chatId, userMessage, rkm));*/
                                break;
                            case "получение статуса":
                                await GetStatus(chatId, userMessage, rkm);
                                /*await Regulator(userMessage, chatId, user, stage, KeyboardMakups.ExitMenu,
                                    async (chatId, userMessage, rkm) =>
                                    await GetStatus(chatId, userMessage, rkm));*/
                                break;
                            case "статистика":
                                await ShowStatictic(chatId, userMessage, rkm);
                                /*await Regulator(userMessage, chatId, user, stage, KeyboardMakups.ExitMenu,
                                    async (chatId, userMessage, rkm) =>
                                    await ShowStatictic(chatId, userMessage, rkm));*/
                                break;
                            default:
                                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDR2HodkrOXJB2TmhnfmVvsP5-RF8pAAIUFQACKlUYApn3W8hHxGLuIwQ");
                                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Это что-то на китайском?", replyMarkup: KeyboardMakups.MainMenu);
                                break;
                        }
                        
                        break;
                }
                
            }
            Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }
        }
        private async Task CheckTime()
        {
            do
            {
                if (DateTime.Now.Hour == 12)
                {
                    await AskUserTasksAsync();
                    await Task.Delay(TimeSpan.FromHours(12));
                }

                if (DateTime.Now.Hour == 18)
                {
                    await AskProgressTasks();
                    await Task.Delay(TimeSpan.FromHours(12));
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            } while (true);

        }
        private async Task Regulator(string userMessage, long chatId, string user, string stage, ReplyKeyboardMarkup rkm, Action <long, string, ReplyKeyboardMarkup> option) // двойная запись rkm?
        {
            var res = KeyboardMakups.Stages.Where(t => t.ToLower().Contains(stage)) is not null;
            if(res)
            {
                option(chatId,
                       userMessage,
                       rkm);
            }
            else
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDR2HodkrOXJB2TmhnfmVvsP5-RF8pAAIUFQACKlUYApn3W8hHxGLuIwQ");
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Это что-то на китайском?", replyMarkup: KeyboardMakups.ExitMenu);
            }
        }
        private async Task RecordUserTasksAsync(long chatId, string userMessage, string user, ReplyKeyboardMarkup rkm)
        {
            ConnectDB.InsertTasks(chatId, userMessage, user);
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Задача {userMessage} принята", replyMarkup: rkm);

        }
        private async Task DeleteUserTasksAsync(long chatId, string userMessage, ReplyKeyboardMarkup rkm)
        {
            var longCheck = long.TryParse(userMessage, out _);
            if(longCheck)
            {
                var result = ConnectDB.DeleteTask(Convert.ToInt32(userMessage));
                if(result)
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Задача {userMessage} удалена", replyMarkup: rkm);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Задача с ID {userMessage} не найдена", replyMarkup: rkm);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Введено некорректное значение", replyMarkup: rkm);
            }

        }
        private async Task AskUserTasksAsync()
        {
            var chats = ConnectDB.GetChats();
            foreach (var chat in chats)
            {
                ConnectDB.UpdateUserStage(chat.chatid, chat.user_name, "добавить задачу");
                await botClient.SendTextMessageAsync(chatId: chat.chatid, text: "Пора составить список задач на день! \nВвод по одной задаче в каждом сообщении", replyMarkup: KeyboardMakups.ExitMenu);
                await botClient.SendStickerAsync(chat.chatid, "CAACAgIAAxkBAAIDFWHobjkXu1-dfswryB4o2VU9eEnqAAMVAAIqVRgCygn9Ylfrq9QjBA");
            }
        }
        private async Task AskProgressTasks()
        {
            var chats = ConnectDB.GetChats();
            foreach (var chat in chats)
            {
                var firstTask = ConnectDB.GetTasks(chat.chatid, DateTime.Now.Date, false).FirstOrDefault();
                if(firstTask is not null)
                {
                    ConnectDB.UpdateUserStage(chat.chatid, chat.user_name, "получение прогресса");
                    ConnectDB.UpdateIterTask(chat.chatid, chat.user_name, firstTask.id);
                    await botClient.SendTextMessageAsync(chatId: firstTask.chatid, text: $"На сегодня Вы поставили задачу {firstTask.task}. Укажите прогресс по задаче от 0 до 100: ", replyMarkup: KeyboardMakups.ExitMenu);
                }
            }
        }
        private async Task AskProgressChat(long chatId)
        {
            var iterTask = ConnectDB.GetIterTask(chatId);
            var tasks = ConnectDB.GetTasks(chatId, DateTime.Now.Date, false);
            foreach (var task in tasks)
            {
                if (task.id > iterTask)
                {
                    ConnectDB.UpdateUserStage(task.chatid, task.user_name, "получение прогресса");
                    ConnectDB.UpdateIterTask(task.chatid, task.user_name, task.id);
                    await botClient.SendTextMessageAsync(chatId: task.chatid, text: $"На сегодня Вы поставили задачу {task.task}. Укажите прогресс по задаче от 0 до 100: ", replyMarkup: KeyboardMakups.ExitMenu);
                    break;
                }
            }
            if(tasks.Count() == 0)
            {
                await PrintProgress(chatId, false);
                ConnectDB.UpdateUserStage(chatId, "", "главное меню");
            }
            
        }
        private async Task GetProgress(long chatId, string userMessage, ReplyKeyboardMarkup rkm)
        {
            var iterTask = ConnectDB.GetIterTask(chatId);
            var tasks = ConnectDB.GetTasks(chatId, DateTime.Now.Date, false);
            var inputValidate = int.TryParse(userMessage, out _);
            if (inputValidate == false)
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDR2HodkrOXJB2TmhnfmVvsP5-RF8pAAIUFQACKlUYApn3W8hHxGLuIwQ");
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Некорректный ввод. Введите число от 0 до 100: ", replyMarkup: KeyboardMakups.ExitMenu);
            }
            foreach (var task in tasks)
            {
                if(task.id == iterTask)
                {
                    ConnectDB.InsertProgress(iterTask, Convert.ToInt32(userMessage));
                    ConnectDB.UpdateUserStage(task.chatid, task.user_name, "получение статуса");
                    await botClient.SendTextMessageAsync(chatId: task.chatid, text: $"Закрыть задачу?", replyMarkup: KeyboardMakups.YesNoMenu);
                    break;
                }
            }
        }
        private async Task GetStatus(long chatId, string userMessage, ReplyKeyboardMarkup rkm)
        {
            var keyWordsPositive = new List<string> { "да", "yes", "закрыть" };
            var keyWordsNegative = new List<string> { "нет", "no", "не надо", "не закрывать", "оставить" };
            var iterTaskID = ConnectDB.GetIterTask(chatId);
            var taskInfo = ConnectDB.GetTask(iterTaskID);

            if (keyWordsPositive.Contains(userMessage))
            {
                ConnectDB.InsertStatus(iterTaskID, true);
                await AskProgressChat(chatId);
            }
            else if (keyWordsNegative.Contains(userMessage))
            {
                ConnectDB.InsertStatus(iterTaskID, false);
                await AskProgressChat(chatId);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"Я Вас не понимаю =(", replyMarkup: KeyboardMakups.ExitMenu); ;
            }

            
        }
        private async Task OpenMainMenu(long chatId, ReplyKeyboardMarkup rkm)
        {
            await botClient.SendTextMessageAsync(chatId: chatId, text: "Главное меню", replyMarkup: rkm);

        }
        private async Task<bool> OpenedTask(long chatId, bool prevDelete)
        {
            var tasks = ConnectDB.GetTasks(chatId, false);
            if (tasks.Count() == 0)
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "Открытых задач не найдено", replyMarkup: KeyboardMakups.MainMenu);
                return false;
            }
            else
            {
                foreach (var task in tasks)
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"ID {task.id} {task.task} открыта {task.date.Date.ToString("dd/MM/yyyy")} с текущим прогрессом {task.progress}", replyMarkup: KeyboardMakups.MainMenu);
                }

                if (prevDelete)
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: "Укажите ID задачи для удаления", replyMarkup: KeyboardMakups.ExitMenu);
                }
                return true;
            }
        }
        private async Task ShowStatictic(long chatId, string userMessage, ReplyKeyboardMarkup rkm)
        {
            var stageInfo = ConnectDB.GetStatisticStage(chatId);
            bool userValidate;
            switch (stageInfo.stage)
            {
                case "получение начальной даты":
                    userValidate = DateTime.TryParse(userMessage, out _);
                    if (userValidate == false)
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Введена некорректная дата", replyMarkup: rkm);
                        break;
                    }
                    ConnectDB.InsertStartDT(chatId, Convert.ToDateTime(userMessage));
                    ConnectDB.UpdateStatisticStage(chatId, stageInfo.user_name, "получение конечной даты");
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"Укажите конечную дату для выгрузки в формате дд.мм.гггг", replyMarkup: rkm);
                    break;
                case "получение конечной даты":
                    userValidate = DateTime.TryParse(userMessage, out _);
                    if (userValidate == false)
                    {
                        await botClient.SendTextMessageAsync(chatId: chatId, text: $"Введена некорректная дата", replyMarkup: rkm);
                        break;
                    }
                    ConnectDB.InsertEndDT(chatId, Convert.ToDateTime(userMessage));
                    await PrintProgress(chatId, true);
                    ConnectDB.DeleteStatisticStage(chatId);
                    break;
            }
            
        }
        private async Task PrintProgress(long chatId, bool statistics)
        {
            double aveProgress = 0;

            if (statistics)
            {
                var stageInfo = ConnectDB.GetStatisticStage(chatId);
                var tasks = ConnectDB.GetTasks(chatId, stageInfo.startdt, stageInfo.enddt);
                if (tasks.Count() == 0)
                {
                    await botClient.SendTextMessageAsync(chatId: chatId, text: $"За пероид задач не найдено", replyMarkup: KeyboardMakups.MainMenu);
                    await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDRWHodkkGpq89STKz_M7HD6XGKzXuAAITFQACKlUYAi5AuG9Mae-DIwQ");
                    return;
                }
                var tasksСlosed = tasks.Where(t => t.closed = true);
                var tasksCount = tasks.Count();
                var countClosed = tasksСlosed.Count();
                aveProgress = tasksСlosed.Average(t => t.progress);
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"За пероид было открыто {tasksCount} задач. " +
                    $"Из них закрыто {countClosed} со средним прогрессом {aveProgress}", replyMarkup: KeyboardMakups.MainMenu);

            }
            else
            {
                var tasksСlosed = ConnectDB.GetTasks(chatId, DateTime.Now.Date, true);
                var tasksOpened = ConnectDB.GetTasks(chatId, false).Count();
                var count = tasksСlosed.Count();
                aveProgress = tasksСlosed.Average(t => t.progress);
                await botClient.SendTextMessageAsync(chatId: chatId, text: $"За день Вы закрыли {count} задач со средним прогрессом {aveProgress}. " +
                    $"Осталось открытых задач {tasksOpened}", replyMarkup: KeyboardMakups.MainMenu);
            }

            if (aveProgress == 100)
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDNWHodj3nv3aSNYpWWnxgF_82GYkpAAILFQACKlUYAi6viknimmoSIwQ");
            }
            else if (aveProgress < 100 & aveProgress >= 70)
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDG2HodigK_alDBtLLfdji7kx5FXZJAAL4FAACKlUYAqE74EVCC1xWIwQ");
            }
            else if(aveProgress < 70 & aveProgress >= 50)
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDLWHodjcebrLZdwcjDQT6v-THyiXrAAIHFQACKlUYAjHnqMeCy-0oIwQ");
            }
            else
            {
                await botClient.SendStickerAsync(chatId, "CAACAgIAAxkBAAIDQ2HodkaULay-bcabjb3aXg9qnsnKAAISFQACKlUYAudarSz7zqfeIwQ");
            }
        }
    }
}
