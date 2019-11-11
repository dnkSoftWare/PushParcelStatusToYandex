using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using FBirdLib;
using FirebirdSql.Data.FirebirdClient;

namespace YandexPUSH
{
    class Parcel
    {
        public uint parcel_id;
        public string parcel_code;
        public uint push_id;
        public uint okod;

        public Parcel(uint parcelId)
        {
            parcel_id = parcelId;
            parcel_code = "";
            push_id = 0;
            okod = 0;
        }
        public void PUSHToYandex()
        {
            // throw new NotImplementedException();
        }

        public bool ReadyToPUSH()
        {
           return this.push_id > 0 && this.okod > 0 && this.parcel_code.Length > 0;
        }
    }
    class Program
    {
        private static List<Parcel> parcels; // Список посылок с изменёнными статусами
        private static string hostname = "testdb";
        private static string dbname = "kur2003";
        //private static List<string> dataBase  = new List<string>(){ } ;

        static void Main(string[] args)
        {
            

            if (GetParcels()) // Получение данных для отправки
            {
                GetDataForMethodPUSH();
            }

           // Console.ReadKey();
        }

        private static void GetDataForMethodPUSH()
        {
            using (var fb = new FBird(true, hostname, dbname ))
            {
                
                foreach (var parcel in parcels)
                {

                    #region Этот код выполняем в одной транзакции

                    FbTransaction tr = fb.GetTransaction(false);
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
                            parcel.PUSHToYandex(); // Кидаем в Яндекс
                            fb.Execute($"execute procedure set_parcel_history_push_1({parcel.push_id}, 1);",
                                ref tr); // Фиксим у себя, что ПУШ отправлен
                        }
                        tr.Commit();

                    }
                    catch (FbException ex)
                    {
                        tr.Rollback(); 
                        // todo: тут код логера
                    }

                    #endregion
                }
                
            }
        }

       

        private static bool GetParcels()
        {
            parcels = new List<Parcel>();
            
            using (var fb = new FBird(autoconnect:true, hostname, dbname))
            {
                var _parcels = fb.Select("select first 3 out_parcel_id from get_parcel_for_push_1(6);").Enumerate();
                foreach (var parcel in _parcels)
                { 
                    var p = new Parcel(UInt32.Parse(parcel[0].ToString()));
                    parcels.Add(p);
                }
            }

            return (parcels.Count > 0);
        }
    }


}
