using Dapper;
using Npgsql;
using Project.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Project
{
    public class ConnectDB : IDisposable
    {
        public IDbConnection _dbConnection = new NpgsqlConnection();
        private bool disposed = false;
     
        public ConnectDB(string connectionString)
        {
            _dbConnection = new NpgsqlConnection(connectionString);
            _dbConnection.Open();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _dbConnection.Close();// Освобождаем управляемые ресурсы
                }
                // освобождаем неуправляемые объекты
                disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        public void InsertChat(long chatid, string user)
        {
            var chatExists = new List<Chat>();
            
            var query = @"select chatid from chats
                        where chatid = @chatid and @user_name = @user_name";

            chatExists =  _dbConnection.Query<Chat>(query, new { chatid = chatid, user_name = user }).ToList();

            if(chatExists.Count ==0)
            {
                query = @"insert into chats (chatid, user_name)
                            values (@chatid, @user)";

                var list = _dbConnection.Execute(query, new
                {
                    chatid = chatid,
                    user = user
                });
            }
        }
        public void UpdateUserStage(long chatid, string user, string stage)
        {
            var chatExists = new List<UserStage>();

            var query = @"select chatid from stage_per_user
                        where chatid = @chatid";

            chatExists = _dbConnection.Query<UserStage>(query, new { chatid = chatid }).ToList();
            

            if (chatExists.Count == 0)
            {
                query = @"insert into stage_per_user (chatid, user_name, stage)
                            values (@chatid, @user, @stage)";

                var list = _dbConnection.Execute(query, new
                {
                    chatid = chatid,
                    user = user,
                    stage = stage
                }); 
            }
            else
            {
                query = @"update stage_per_user set stage = @stage where chatid = @chatid";

                var list = _dbConnection.Execute(query, new
                {
                    stage = stage,
                    chatid = chatid
                });
            }
        }
        public string GetUserStage(long chatid)
        {
            var query = @"select stage from stage_per_user
                        where chatid = @chatid";

            return _dbConnection.Query<UserStage>(query, new { chatid = chatid }).ToList()[0].stage;
        }
        public void InsertTasks(long chatid, string task, string user)
        {
            var query = @"insert into tasks (date, task, chatid, user_name)
                            values ( @date, @task, @chatid, @user_name)";

            var list = _dbConnection.Execute(query, new
            {
                date = DateTime.Now.Date,
                task = task,
                chatid = chatid,
                user_name =user
            });
        }
        public void UpdateIterTask(long chatid, string user, long iterTaskId)
        {
            var chatExist = new IterUserTask();
            var query = @"select * from iter_user_task where chatid = @chatid";

            chatExist = _dbConnection.Query<IterUserTask>(query, new { chatid = chatid }).ToList().FirstOrDefault();
            

            if (chatExist is null)
            {
                query = @"insert into iter_user_task (chatid, user_name, iter_task_id)
                            values (@chatid, @user_name, @iter_task_id)";

                var list = _dbConnection.Execute(query, new
                {
                    chatid = chatid,
                    user_name = user,
                    iter_task_id = iterTaskId
                });
                
            }
            else
            {
                query = @"update iter_user_task set iter_task_id = @iter_task_id where chatid = @chatid";
                var list = _dbConnection.Execute(query, new
                {
                    iter_task_id = iterTaskId,
                    chatid = chatid
                });
            }
        }
        public long GetIterTask(long chatid)
        {
            var query = @"select * from iter_user_task where chatid = @chatid";

            return _dbConnection.Query<IterUserTask>(query, new { chatid = chatid }).ToList().FirstOrDefault().iter_task_id;

        }
        public void InsertProgress(long id, long progress)
        {
            var query = @"update tasks set progress = @progress where id = @id";
            var list = _dbConnection.Execute(query, new
            {
                progress = progress,
                id = id
            });
            
        }
        public void InsertStatus(long id, bool userMessage)
        {
            var query = @"update tasks set closed = @closed where id = @id";

            var list = _dbConnection.Execute(query, new
            {
                closed = userMessage,
                id = id
            });
        }
        public IEnumerable<Chat> GetChats()
        {
            var query = @"select * from chats";

            return _dbConnection.Query<Chat>(query);

        }
        public IEnumerable<UserTask> GetTasks(long chatid, DateTime date)
        {
            var query = @"select date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and date = @date";

            return _dbConnection.Query<UserTask>(query, new {chatid = chatid, date = DateTime.Now.Date}).OrderBy(t => t.id);

            
        }
        public IEnumerable<UserTask> GetTasks(long chatid, DateTime date, bool closed)
        {
            var query = @"select id, date, task, progress, closed, chatid, user_name from tasks
                        where chatid = @chatid and date = @date and closed = @closed" ;

            return _dbConnection.Query<UserTask>(query, new { 
                chatid = chatid, 
                date = DateTime.Now.Date,
                closed = closed
            }).OrderBy(t => t.id);

            
        }
        public IEnumerable<UserTask> GetTasks(long chatid, bool closed)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and closed = @closed";

            return _dbConnection.Query<UserTask>(query, new { chatid = chatid, closed = closed }).OrderBy(t => t.id);

        }
        public IEnumerable<UserTask> GetTasks(long chatid, DateTime dateStart, DateTime dateFinish)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where chatid = @chatid and date between  @dateStart and @dateFinish";

            return _dbConnection.Query<UserTask>(query, new 
            { 
                chatid = chatid,
                dateStart = dateStart,
                dateFinish = dateFinish,
            }).OrderBy(t => t.id);

            
        }
        public UserTask GetTask(long id)
        {
            var query = @"select id, date, task, progress, closed, chatid from tasks
                        where id = @id";

            return _dbConnection.Query<UserTask>(query, new { id = id }).FirstOrDefault();

        }
        public bool DeleteTask(long id)
        {
            var taskExists = new List<UserTask>();

            var query = @"select task from tasks
                        where id = @id";

            taskExists = _dbConnection.Query<UserTask>(query, new { id = id }).ToList();
            
            if (taskExists.Count == 0)
            {
                return false;
            }
            else
            {
                query = @"delete from tasks where id = @id";

                _dbConnection.Query<UserTask>(query, new { id = id });

                return true;
            }
        }
        public void UpdateStatisticStage(long chatid, string user, string stage)
        {
            var chatExists = new List<StatisticsStage>();

            var query = @"select chatid from ask_statistics_stage
                        where chatid = @chatid";

            chatExists = _dbConnection.Query<StatisticsStage>(query, new { chatid = chatid }).ToList();
            

            if (chatExists.Count == 0)
            {
                query = @"insert into ask_statistics_stage (chatid, user_name, stage)
                            values (@chatid, @user, @stage)";

                var list = _dbConnection.Execute(query, new
                {
                    chatid = chatid,
                    user = user,
                    stage = stage
                });

            }
            else
            {
                query = @"update ask_statistics_stage set stage = @stage where chatid = @chatid";

                var list = _dbConnection.Execute(query, new
                {
                    stage = stage,
                    chatid = chatid
                });
                
            }
        }
        public StatisticsStage GetStatisticStage(long chatid)
        {
            var query = @"select * from ask_statistics_stage
                        where chatid = @chatid";

            return _dbConnection.Query<StatisticsStage>(query, new { chatid = chatid }).FirstOrDefault();
            
        }
        public void InsertStartDT(long chatid, DateTime date)
        {
            var query = @"update ask_statistics_stage set startdt = @startdt where chatid = @chatid";

            var list = _dbConnection.Execute(query, new
            {
                chatid = chatid,
                startdt = date
            });
        }
        public void InsertEndDT(long chatid, DateTime date)
        {
            var query = @"update ask_statistics_stage set enddt = @enddt where chatid = @chatid";
            var list = _dbConnection.Execute(query, new
            {
                chatid = chatid,
                enddt = date
            });
        }
        public void DeleteStatisticStage(long chatid)
        {
            var query = @"delete from ask_statistics_stage where chatid = @chatid";

            _dbConnection.Query<StatisticsStage>(query, new { chatid = chatid });
            
        }
    }
}
