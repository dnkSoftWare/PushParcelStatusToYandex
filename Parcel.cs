using System;
using System.Collections.Generic;
using NLog;

namespace YandexPUSH
{
    public class Parcel
    {
        public uint parcel_id;
        public string parcel_code;
        public uint push_id;
        public uint okod;
        public int error_sended; // Ошибка при отправлении

        public Parcel(uint parcelId)
        {
            parcel_id = parcelId;
            parcel_code = "";
            push_id = 0;
            okod = 0;
            error_sended = 0;
        }

        public bool ReadyToPUSH()
        {
            return this.push_id > 0 && this.okod > 0 && this.parcel_code.Length > 0;
        }

        public bool PUSHToYandex(ILogger logger, List<Parcel> parcels)
        {
            var res = true;
            using (var yndApi = new YandexAPI(logger))
            {

                yndApi.BaseAddress = new Uri($"https://api-logistic.vs.market.yandex.net/delivery/query-gateway"); 

                foreach (var parcel in parcels)
                {
                    res = res && yndApi.PushOrdersStatusesChanged(parcel);
                }
                
            }

            return res; // Отправка успешная, если каждая посылка отправилась корректно
        }

        public override string ToString()
        {
            return $"{nameof(parcel_id)}: {parcel_id}, {nameof(parcel_code)}: {parcel_code}, {nameof(push_id)}: {push_id}, {nameof(okod)}: {okod}";
        }
    }
}