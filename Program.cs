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

            Console.ReadKey();
        }

        private static void GetDataForMethodPUSH()
        {
            using (var fb = new FBird(true, hostname, dbname ))
            {
                
                foreach (var parcel in parcels)
                {
                   // var data = new PushData(0, 0, "");
                   FbTransaction tr = fb.GetTransaction(false);
                  var data = fb.Select(
                        $"select out_push_id, out_otpravka_kod, out_parcel_code from create_push_by_parcel_1({parcel.parcel_id});", ref tr).Enumerate().ToArray();
//                  foreach (var d in data)
//                  {
                      if (!string.IsNullOrEmpty(data[0]["out_push_id"].ToString()))
                      {
                          PUSHToYandex(data[0]["out_otpravka_kod"].ToString(), data[0]["out_parcel_code"].ToString());
                          fb.Execute("execute procedure set_parcel_history_push_1(:p_push_id, :p_push_status);", ref tr);
                      }
                  //}
                  
                   tr.Commit();
                }
                
            }
        }

        private static void PUSHToYandex(string otpravka_kod, string parcel_code)
        {
           // throw new NotImplementedException();
        }

        private static bool GetParcels()
        {
            parcels = new List<Parcel>();
            
            using (var fb = new FBird(autoconnect:true, hostname, dbname))
            {
                var _parcels = fb.Select("select out_parcel_id from get_parcel_for_push_1(6);").Enumerate();
                foreach (var parcel in _parcels)
                { 
                    var p = new Parcel(UInt32.Parse(parcel[0].ToString()));
                    parcels.Add(p);
                }
            }

            return (parcels.Count > 0);
        }
    }

    internal class PushData
    {
        public uint out_push_id;
        public uint out_otpravka_kod;
        public string out_parcel_code;

        public PushData(uint outPushId, uint outOtpravkaKod, string outParcelCode)
        {
            out_push_id = outPushId;
            out_otpravka_kod = outOtpravkaKod;
            out_parcel_code = outParcelCode;
        }
    }
}
