using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FBirdLib;
using FirebirdSql.Data.FirebirdClient;
using NLog;
//using YandexPUSH.Parcel;


namespace YandexPUSH
{
    class Program
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static List<Parcel> parcels; // Список посылок с изменёнными статусами
        private static string hostname = "proddb";
        private static string dbname = "kur2003";
        private static int principalId = 6;

        static void Main(string[] args)
        {
            try
            { 
                logger.Info("----===== Старт =====----");
                if (GetParcels($"select out_parcel_id from get_parcel_for_push_1({principalId});")) // Получение данных для отправки
                {
                    logger.Info($"Получено уведомлений по изменению статусов: {parcels.Count} ");
                    GetDataAndPush(); // Собственно отправка

                    SendError(30, 10);

                }

            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
            }
            finally
            {
                logger.Info("----======= Финиш =======----");
                NLog.LogManager.Shutdown();
            }
        }

        private static void GetDataAndPush()
        {
            using (var fb = new FBird(true, hostname, dbname ))
            {
                FbTransaction tr = fb.GetTransaction(false); // Танзакция будет одна на весь список
                
                    #region Этот код выполняем в одной транзакции 

                    foreach (var parcel in parcels)
                    {
                        try
                        {

                            var data = fb.Select(
                                $"select out_push_id, out_otpravka_kod, out_parcel_code from create_push_by_parcel_1({parcel.parcel_id});",
                                ref tr).Enumerate();
                            foreach (var d in data)
                            {
                                parcel.okod = uint.Parse(d["out_otpravka_kod"].ToString());
                                parcel.push_id = uint.Parse(d["out_push_id"].ToString());
                                parcel.parcel_code = d["out_parcel_code"].ToString();
                            }

                            if (parcel.ReadyToPUSH())
                            {
                                var localParcel = new List<Parcel>();
                                localParcel.Add(parcel);

                                if (parcel.PUSHToYandex(logger, localParcel)) // Кидаем в Яндекс
                                {
                                    fb.Execute($"execute procedure set_parcel_history_push_1({parcel.push_id}, 1);",
                                        ref tr /*подхватили ранее открытую транзакцию*/); // Фиксим у себя, что ПУШ отправлен

                                    tr.CommitRetaining(); // Сюда дошли, значит всё ок коммитимся
                                    logger.Info($"Успех [{parcel.ToString()}]");
                                }
                                else
                                {
                                    parcel.error_sended = 1;
                                    tr.RollbackRetaining();
                                    logger.Warn($"Проблема при отправке заказа:{parcel.ToString()}");
                                }
                            }
                            else
                            {
                                tr.RollbackRetaining();
                                logger.Warn($"Проблема при получении данных по заказу:{parcel.ToString()}");
                            }

                        }
                        catch (FbException ex)
                        {
                            parcel.error_sended = 2; // Fatal error
                            tr.RollbackRetaining();
                            logger.Error($"Error:{ex.Message}");
                        }


                    }

                    #endregion
                
            }
        }

        /// <summary>
        /// Получение списка заказов по которым изменились статусы 
        /// </summary>
        /// <param name="selectOutParcelIdFromGetParcelForPush"></param>
        /// <returns>true если есть данные в списке</returns>
        private static bool GetParcels(string selectOutParcelIdFromGetParcelForPush)
        {
            parcels = new List<Parcel>();
            
            using (var fb = new FBird(autoconnect:true, hostname, dbname))
            {
                var _parcels = fb.Select(selectOutParcelIdFromGetParcelForPush).Enumerate();
                foreach (var parcel in _parcels)
                { 
                    var p = new Parcel(UInt32.Parse(parcel[0].ToString()));
                    parcels.Add(p);
                }
            }

            return (parcels.Count > 0);
        }

        /// <summary>
        ///  отправка отчета об ошибках за указанное кол-во минут
        /// </summary>
        /// <param name="minuten">Кол-во минут буферизации ошибок </param>
        /// <param name="percent">Процент ошибок при отправке</param>
        private static void SendError(int minuten, int percent)
        {
            double percent1 = (double)parcels.Count / 100;
            double percent10 = (percent1 * percent);
            double error_percent = (double)parcels.Count(p => p.error_sended > 0) / percent1;

            var log_message = $"{DateTime.Now.ToLocalTime()} Статистика:" +
                              $"\r\n\t - получено уведомлений по изменению статусов:{parcels.Count}" +
                              $"\r\n\t - отправлено успешно уведомлений:{parcels.Count(p => p.error_sended == 0)}" +
                              $"\r\n\t - получено ошибок при отправлении:{parcels.Count(p => p.error_sended > 0)}" +
                              $"\r\n\t   Что составляет: {Math.Round(error_percent, 2)} % \r\n";
            if (parcels.Count(p => p.error_sended > 0) > (percent10))
            {
                var folder = AppDomain.CurrentDomain.BaseDirectory + "Tmp";
                 if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                 var file_error  = Path.Combine(folder, @"Errors.txt");

                File.AppendAllText(file_error, log_message);

                if (File.Exists(file_error))
                {
                    TimeSpan ts = DateTime.Now - File.GetCreationTime(file_error); //TimeSpan.TicksPerMinute
                     if ( ts.Minutes > minuten )
                     {
                        logger.Error(File.ReadAllText(file_error));
                        Thread.Sleep(2000);
                        File.Delete(file_error);
                     }
                }
            }
        }
    }


}
