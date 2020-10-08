﻿using GemBox.Document;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace QUESTionBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static TelegramBotClient botClient;
        public User botInfo;
        public static Dictionary<long, Team> teamList = new Dictionary<long, Team>();
        static Dictionary<string, Task> taskList;
        


        public MainWindow()
        {
            InitializeComponent();           
            botClient = new TelegramBotClient("1379007033:AAF6K0EW8z8E9GGytASmSX0BwLDngGkIQnA");
            botInfo = botClient.GetMeAsync().Result;
            taskList = Task.CreateTaskList();
            debugTextBlock.Text += $"Здравствуй, мир! Я бот по имени {botInfo.FirstName} и мой ID: {botInfo.Id} \nЯ готов приступить к работе.";
            botStopButton.IsEnabled = false;
        }

        //кнопки запуска Бота в программе
        private void botLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (botClient.IsReceiving == false)
            {
                botClient.MessageOffset = -1;
                botClient.OnMessage += Bot_OnMessage;
                botClient.OnCallbackQuery += BotOnCallbackQueryReceived;
                botClient.StartReceiving();
                debugTextBlock.Text += "\nБот начал принимать сообщения.";
                botStopButton.IsEnabled = true;
                loadButton.IsEnabled = false;
                botLaunchButton.IsEnabled = false;
            }
        }

        private void botStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (botClient.IsReceiving == true)
            {
                botClient.StopReceiving();
                debugTextBlock.Text += "\nБот перестал принимать сообщения.";
                botLaunchButton.IsEnabled = true;
                loadButton.IsEnabled = true;
                botStopButton.IsEnabled = false;
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if (botClient.IsReceiving == false)
            {
                teamList = DB.LoadData();
                MessageBox.Show("База данных загружена");
            }
        }

        private async void startWorkingButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<long, Team> keyValue in teamList)
            {
                if ((keyValue.Value.QuestStartedAt != null)&&(keyValue.Value.QuestFinishedAt==null))
                {
                    await botClient.SendTextMessageAsync(
                          chatId: keyValue.Value.LinkedChat,
                          parseMode: ParseMode.Markdown,
                          text: "Капитаны! Бот возвращается! Меня за-за-запустят в течение минуты! По её прошествии смело продолжайте квест!"
                        );
                }
            }
        }

        private async void alarmBrokenButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (KeyValuePair<long, Team> keyValue in teamList)
            {
                if ((keyValue.Value.QuestStartedAt != null) && (keyValue.Value.QuestFinishedAt == null))
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(
                          chatId: keyValue.Value.LinkedChat,
                          parseMode: ParseMode.Markdown,
                          text: "Капитаны! У бота технические непо-непо-неполадки!... Я вернусь в течение 3-5 минут... Не пишите мне, пока я вам сам не скажу!" +
                          "\n Если я за-за-задержусь, свяжитесь, пожалуйста, с @katchern!"
                        );
                    }
                    catch
                    {

                    }
                }
            }
        }

        public async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text == null) 
            {

                    Message message = await botClient.SendTextMessageAsync(
                      chatId: e.Message.Chat,
                      parseMode: ParseMode.Markdown,
                      text: "Либо вы пишете мне неправильный ответ, либо я не могу распознать вашей команды. Попробуйте ещё раз!" +
                      "\nЕсли ситуация тупиковая, напишите @katchern и вам подскажут, что делать."
                    );
                    this.Dispatcher.Invoke(() =>
                    {
                        debugTextBlock.Text += $"\n{ message.From.FirstName} отправил сообщение { message.MessageId } " +
                        $"в чат {message.Chat.Id} в {message.Date}. " +
                        $"Это ответ на сообщение {e.Message.MessageId}. Команда участника не была распознана.";
                    });
                    return;
                
            }
            // стартовый пак
            if (e.Message.Text == "/start")
            {

                Message message1 = await botClient.SendTextMessageAsync(
                  chatId: e.Message.Chat,
                  text: TextTemplates.message1
                );



                Thread.Sleep(4000);
                Message message2 = await botClient.SendTextMessageAsync(
                    chatId: e.Message.Chat,
                    text: TextTemplates.message2,
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Согласен/согласна", "agreement"))
                    );

                this.Dispatcher.Invoke(() =>
                {
                    debugTextBlock.Text += $"\nОтправлено приветственное сообщение { message1.MessageId} " +
                    $"в чат {message1.Chat.Id} в {message1.Date.ToLocalTime()}. ";
                });

            }
            // если человек прислал ключик
            else if (Team.KeyWordsList.Contains(e.Message.Text.Trim().ToLower()))
            {
                if (!teamList.ContainsKey(e.Message.Chat.Id))
                {
                    teamList.Add(e.Message.Chat.Id, new Team(Team.KeyWordsList.ToList().IndexOf(e.Message.Text) + 1));

                    teamList[e.Message.Chat.Id].LinkedChat = e.Message.Chat.Id;

                    if (!DB.TeamAdd(teamList[e.Message.Chat.Id], (e.Message.Text.Trim().ToLower())))
                    {
                        await botClient.SendTextMessageAsync(
                                            chatId: e.Message.Chat,
                                            text: $"Команда № {teamList[e.Message.Chat.Id].TeamID} уже ввела этот ключ. Если вы являетесь членом команды" +
                                            $"и пытаетесь сменить капитана, обратитесь к организатору квеста (@katchren)",
                                            parseMode: ParseMode.Markdown
                                            );
                        teamList.Remove(e.Message.Chat.Id);
                        return;
                    };

                    Message message = await botClient.SendTextMessageAsync(
                                            chatId: e.Message.Chat,
                                            text: $"Команда № {teamList[e.Message.Chat.Id].TeamID}, ваше время пошло. Первая станция во вложении ниже. *К*",
                                            parseMode: ParseMode.Markdown
                                            );



                    BetweenTaskInteraction(teamList[e.Message.Chat.Id]);

                    this.Dispatcher.Invoke(() =>
                    {
                        debugTextBlock.Text += $"\nОтправлена инструкция { message.MessageId} " +
                        $"в чат {message.Chat.Id} в {message.Date.ToLocalTime()}. " +
                        $"Команда номер {teamList[e.Message.Chat.Id].TeamID} успешно ввела свой ключ и получила задания.";
                    });
                }
                else if (teamList[e.Message.Chat.Id].LinkedChat == e.Message.Chat.Id)
                {
                    Message message = await botClient.SendTextMessageAsync(
                                        chatId: e.Message.Chat,
                                        text: $"Необязательно присылать мне ключ во второй раз. " +
                                        $"Я уже знаю, что вы представляете команду номер {teamList[e.Message.Chat.Id].TeamID}."
                                        );
                    this.Dispatcher.Invoke(() =>
                    {
                        debugTextBlock.Text += $"Отправлено сообщение { message.MessageId} " +
                        $"в чат {message.Chat.Id} в {message.Date.ToLocalTime()}. " +
                        $"Это ответ на сообщение {e.Message.MessageId}. " +
                        $"Команда номер {teamList[e.Message.Chat.Id].TeamID} повторно ввела свой ключ.";
                    });
                }
                else
                {
                    Message message = await botClient.SendTextMessageAsync(
                                        chatId: e.Message.Chat,
                                        replyToMessageId: e.Message.MessageId,
                                        text: $"К сожалению, Этот ключ был введён ранее другой командой. Может, кто-либо из вашей команды уже является капитаном? " +
                                        $"Если нет, и вы уверены, что этот ключ именно ваш, то обратитесь к организаторам."
                                        );
                    this.Dispatcher.Invoke(() =>
                    {
                        debugTextBlock.Text += $"\nОтправлено сообщение { message.MessageId} " +
                        $"в чат {message.Chat.Id} в {message.Date.ToLocalTime()}. " +
                        $"Это ответ на сообщение {e.Message.MessageId}. " +
                        $"Ключ был отклонён, поскольку команда номер {teamList[e.Message.Chat.Id].TeamID} уже занята.";
                    });
                }
            }
            // приём ответов на вопросы-триггеры
            else if (Task.KeyPhrasesList.Contains(e.Message.Text.Trim().ToLower()) || (e.Message.Text.Trim().ToLower() == "розовые") || (e.Message.Text.Trim().ToLower() == "фиолетовый") || (e.Message.Text.Trim().ToLower() == "фиолетовые"))
            {
                if ((e.Message != null) && (e.Message.Chat != null))
                {
                    if (e.Message.Text.Trim().ToLower() == "хармс")
                    {
                        teamList[e.Message.Chat.Id].CurrentQuestion++;
                    }
                    Task.TaskInteraction(teamList[e.Message.Chat.Id]);
                }


                //this.Dispatcher.Invoke(() =>
                //{
                //    debugTextBlock.Text += $"\nОтправлено сообщение { message.MessageId} " +
                //    $"в чат {message.Chat.Id} в {message.Date.ToLocalTime()}. " +
                //    $"Это ответ на сообщение {e.Message.MessageId}. " +
                //    $"Команда номер {teamList[e.Message.Chat.Id].TeamID} верно ввела ответ на триггер номер {Task.KeyPhrasesList.ToList().IndexOf(e.Message.Text)+1}";
                //});
            }
            // приём триггера вопросов внутри станции
            //else if (Task.QuestionTriggers.Contains(e.Message.Text.Trim().ToLower()))
            //{
            //    Task.TriggerHandler(e.Message.Text, teamList[e.Message.Chat.Id], e.Message.Chat.Id);
            //}
            //приём "мы готовы" на задании с Лениным
            else if ((e.Message.Text.Trim().ToLower() == "мы готовы") && (teamList[e.Message.Chat.Id].CurrentStation == 7))
            {
                BetweenTaskInteraction(teamList[e.Message.Chat.Id]);
            }
            // дефолтный ответ на нераспознанную команду
            else if (e.Message.Text != null)
            {
                
                if (MainWindow.teamList[e.Message.Chat.Id].noWrongAnswer)
                {
                    DB.AddAnswer(teamList[e.Message.Chat.Id], e.Message.Text);
                    teamList[e.Message.Chat.Id].CurrentQuestion++;
                    DB.UpdateTeamNote(teamList[e.Message.Chat.Id]);
                    Task.TaskInteraction(teamList[e.Message.Chat.Id]);
                }
                else
                {

                    Message message = await botClient.SendTextMessageAsync(
                      chatId: e.Message.Chat,
                      parseMode: ParseMode.Markdown,
                      text: "Либо вы пишете мне неправильный ответ, либо я не могу распознать вашей команды. Попробуйте ещё раз!"+
                      "\nЕсли ситуация тупиковая, напишите @katchern и вам подскажут, что делать."
                    );
                    this.Dispatcher.Invoke(() =>
                    {
                        debugTextBlock.Text += $"\n{ message.From.FirstName} отправил сообщение { message.MessageId } " +
                        $"в чат {message.Chat.Id} в {message.Date}. " +
                        $"Это ответ на сообщение {e.Message.MessageId}. Команда участника не была распознана.";
                    });
                }
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            Team team = teamList[callbackQuery.Message.Chat.Id];
            long chatId = callbackQuery.Message.Chat.Id;
            int lastMessageId = team.lastBotMessage.MessageId;

            if ((callbackQuery.Data != "hint")&&(callbackQuery.Data != "agreement"))
            {
                await botClient.EditMessageReplyMarkupAsync(chatId: chatId, lastMessageId);
            }

            switch (callbackQuery.Data)
            {
                case ("agreement"):
                    await botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: $"Спасибо, что цените установленные правила!"
                    );

                    Thread.Sleep(2000);
                    await botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: TextTemplates.message3
                    );

                    Thread.Sleep(3000);
                    await botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: TextTemplates.message4,
                        parseMode: ParseMode.Markdown
                    );

                    Thread.Sleep(3000);
                    await botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: TextTemplates.message5
                    );
                    break;
                case ("nexttask"):
                    
                    team.CurrentQuestion++;
                    DB.UpdateTeamNote(team);
                    Task.TaskInteraction(team);
                    break;
                case ("right"):
                    
                    team.Points++;
                    DB.AddAnswer(team, "верно");
                    team.CurrentQuestion++;
                    DB.UpdateTeamNote(team);
                    Task.TaskInteraction(team);
                    break;
                case ("wrong"):
                    
                    DB.AddAnswer(team, "неверно");
                    team.CurrentQuestion++;
                    DB.UpdateTeamNote(team);
                    Task.TaskInteraction(team);
                    break;
                case ("hint"):
                    Task.HintHandler(team);
                    break;
                case ("questend"):
                    team.QuestFinishedAt = DateTime.Now.ToLocalTime();
                    DB.UpdateTeamNote(team);
                    
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: TextTemplates.message97
                    );
                    Thread.Sleep(2000);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: TextTemplates.message98
                    );
                    Thread.Sleep(2000);
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: TextTemplates.message99,
                        parseMode: ParseMode.Markdown
                    );
                    Thread.Sleep(2000);
                    using (var stream = System.IO.File.OpenRead("D:\\Other\\BotMediaFiles\\hashtag.png"))
                    {
                        await botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: stream,
                            caption: TextTemplates.message100
                        );
                    }
                    break;
                default:
                    break;
            }
        }

        public static async void BetweenTaskInteraction(Team team)
        {
            team.CurrentQuestion = 0;
            team.CurrentStation++;
            DB.UpdateTeamNote(team);
            if (team.CurrentStation == 10) 
            {
                await MainWindow.botClient.SendTextMessageAsync(
                                        chatId: team.LinkedChat,
                                        text: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].MessageTrigger
                                        );
            }
            else
            {
                await MainWindow.botClient.SendVenueAsync(chatId: team.LinkedChat,
                                                    latitude: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].LinkedLocation.Latitude,
                                                    longitude: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].LinkedLocation.Longitude,
                                                    title: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].Title,
                                                    address: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].Address
                                                   );
                await MainWindow.botClient.SendTextMessageAsync(
                                        chatId: team.LinkedChat,
                                        text: taskList[Task.KeyPhrasesList[team.CurrentStation - 1]].MessageTrigger
                                        );
            }
        }

        
    }
}
