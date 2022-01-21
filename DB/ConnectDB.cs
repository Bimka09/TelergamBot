using Dapper;
using Npgsql;
using Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project
{
    static class ConnectDB
    {
        public static string SqlConnectionString = "User ID=postgres;Password=k1t2i3f4;Host=localhost;Port=5432;Database=ToDoBot;";
        public static void InsertChat(long chatid, string user)
        {
            var chatExists = new List<Chat>();
            
            var query = @"select chatid from chats
                        where chatid = @chatid and @user_name = @user_name";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                chatExists =  connection.Query<Chat>(query, new { chatid = chatid, user_name = user }).ToList();
            }

            if(chatExists.Count ==0)
            {
                query = @"insert into chats (chatid, user_name)
                            values (@chatid, @user)";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        chatid = chatid,
                        user = user
                    });
                }
            }

        }
        public static void UpdateUserStage(long chatid, string user, string stage)
        {
            var chatExists = new List<UserStage>();

            var query = @"select chatid from stage_per_user
                        where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                chatExists = connection.Query<UserStage>(query, new { chatid = chatid }).ToList();
            }

            if (chatExists.Count == 0)
            {
                query = @"insert into stage_per_user (chatid, user_name, stage)
                            values (@chatid, @user, @stage)";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        chatid = chatid,
                        user = user,
                        stage = stage
                    });
                }
            }
            else
            {
                query = @"update stage_per_user set stage = @stage where chatid = @chatid";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        stage = stage,
                        chatid = chatid
                    });
                }
            }
        }
        public static string GetUserStage(long chatid)
        {
            var query = @"select stage from stage_per_user
                        where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserStage>(query, new { chatid = chatid }).ToList()[0].stage;
            }
        }
        public static void InsertTasks(long chatid, string task, string user)
        {
            var query = @"insert into tasks (date, task, chatid, user_name)
                            values ( @date, @task, @chatid, @user_name)";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Execute(query, new
                {
                    date = DateTime.Now.Date,
                    task = task,
                    chatid = chatid,
                    user_name =user
                });
            }
        }
        public static void UpdateIterTask(long chatid, string user, long iterTaskId)
        {
            var chatExist = new IterUserTask();
            var query = @"select * from iter_user_task where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                chatExist = connection.Query<IterUserTask>(query, new { chatid = chatid }).ToList().FirstOrDefault();
            }

            if (chatExist is null)
            {
                query = @"insert into iter_user_task (chatid, user_name, iter_task_id)
                            values (@chatid, @user_name, @iter_task_id)";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        chatid = chatid,
                        user_name = user,
                        iter_task_id = iterTaskId
                    });
                }
            }
            else
            {
                query = @"update iter_user_task set iter_task_id = @iter_task_id where chatid = @chatid";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        iter_task_id = iterTaskId,
                        chatid = chatid
                    });
                }
            }
        }
        public static long GetIterTask(long chatid)
        {
            var query = @"select * from iter_user_task where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<IterUserTask>(query, new { chatid = chatid }).ToList().FirstOrDefault().iter_task_id;
            }
        }
        public static void InsertProgress(long id, long progress)
        {
            var query = @"update tasks set progress = @progress where id = @id";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Execute(query, new
                {
                    progress = progress,
                    id = id
                });
            }
        }
        public static void InsertStatus(long id, bool userMessage)
        {
            var query = @"update tasks set closed = @closed where id = @id";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Execute(query, new
                {
                    closed = userMessage,
                    id = id
                });
            }
        }
        public static IEnumerable<Chat> GetChats()
        {
            var query = @"select * from chats";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<Chat>(query);
            }
        }
        public static IEnumerable<UserTask> GetTasks(long chatid, DateTime date)
        {
            var query = @"select date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and date = @date";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserTask>(query, new {chatid = chatid, date = DateTime.Now.Date}).OrderBy(t => t.id);

            }
        }
        public static IEnumerable<UserTask> GetTasks(long chatid, DateTime date, bool closed)
        {
            var query = @"select id, date, task, progress, closed, chatid, user_name from tasks
                        where chatid = @chatid and date = @date and closed = @closed" ;
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserTask>(query, new { 
                    chatid = chatid, 
                    date = DateTime.Now.Date,
                    closed = closed
                }).OrderBy(t => t.id);

            }
        }
        public static IEnumerable<UserTask> GetTasks(long chatid, bool closed)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and closed = @closed";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserTask>(query, new { chatid = chatid, closed = closed }).OrderBy(t => t.id);

            }
        }
        public static IEnumerable<UserTask> GetTasks(long chatid, DateTime dateStart, DateTime dateFinish)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and date between  @dateStart and @dateFinish";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserTask>(query, new 
                { 
                    chatid = chatid,
                    dateStart = dateStart,
                    dateFinish = dateFinish,
                }).OrderBy(t => t.id);

            }
        }
        public static UserTask GetTask(long id)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where id = @id";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<UserTask>(query, new { id = id }).FirstOrDefault();

            }
        }
        public static bool DeleteTask(long id)
        {
            var taskExists = new List<UserTask>();

            var query = @"select task from tasks
                        where id = @id";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                taskExists = connection.Query<UserTask>(query, new { id = id }).ToList();
            }
            if (taskExists.Count == 0)
            {
                return false;
            }
            else
            {
                query = @"delete from tasks where id = @id";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    connection.Query<UserTask>(query, new { id = id });
                }
                return true;
            }
        }
        public static void UpdateStatisticStage(long chatid, string user, string stage)
        {
            var chatExists = new List<StatisticsStage>();

            var query = @"select chatid from ask_statistics_stage
                        where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                chatExists = connection.Query<StatisticsStage>(query, new { chatid = chatid }).ToList();
            }

            if (chatExists.Count == 0)
            {
                query = @"insert into ask_statistics_stage (chatid, user_name, stage)
                            values (@chatid, @user, @stage)";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        chatid = chatid,
                        user = user,
                        stage = stage
                    });
                }
            }
            else
            {
                query = @"update ask_statistics_stage set stage = @stage where chatid = @chatid";
                using (var connection = new NpgsqlConnection(SqlConnectionString))
                {
                    var list = connection.Execute(query, new
                    {
                        stage = stage,
                        chatid = chatid
                    });
                }
            }
        }
        public static StatisticsStage GetStatisticStage(long chatid)
        {
            var query = @"select * from ask_statistics_stage
                        where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                return connection.Query<StatisticsStage>(query, new { chatid = chatid }).FirstOrDefault();
            }
        }
        public static void InsertStartDT(long chatid, DateTime date)
        {
            var query = @"update ask_statistics_stage set startdt = @startdt where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Execute(query, new
                {
                    chatid = chatid,
                    startdt = date
                });
            }
        }
        public static void InsertEndDT(long chatid, DateTime date)
        {
            var query = @"update ask_statistics_stage set enddt = @enddt where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                var list = connection.Execute(query, new
                {
                    chatid = chatid,
                    enddt = date
                });
            }
        }
        public static void DeleteStatisticStage(long chatid)
        {
            var query = @"delete from ask_statistics_stage where chatid = @chatid";
            using (var connection = new NpgsqlConnection(SqlConnectionString))
            {
                connection.Query<StatisticsStage>(query, new { chatid = chatid });
            }
        }
    }
}
