using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace Project
{
    public static class KeyboardMakups
    {
        public static List<string> Stages = new List<string>() { "Добавить задачу", "Удалить задачу", 
            "Открытые задачи", "Статистика", "получение прогресса", "получение статуса"};
        public static ReplyKeyboardMarkup MainMenu = new ReplyKeyboardMarkup(new[] {
        new[]{
            new KeyboardButton("Добавить задачу"),
            new KeyboardButton("Удалить задачу"),
        },
        new[]{
            new KeyboardButton("Открытые задачи"),
            new KeyboardButton("Статистика"),
        } });

        public static ReplyKeyboardMarkup ExitMenu = new ReplyKeyboardMarkup(new[] {
        new[]{
            new KeyboardButton("Главное меню")
        }});

        public static ReplyKeyboardMarkup YesNoMenu = new ReplyKeyboardMarkup(new[] {
        new[]{
            new KeyboardButton("Да"),
            new KeyboardButton("Нет"),
        }});
    }
}
