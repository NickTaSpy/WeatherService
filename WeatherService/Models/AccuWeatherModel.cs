﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeatherService.WeatherProviders;

namespace WeatherService.Models
{
    public class AccuWeatherModel
    {
        public class Headline
        {
            public string EffectiveDate;
            public long EffectiveEpochDate;
            public int Severity;
            public string Text;
            public string Category;
            public string EndDate;
            public long? EndEpochDate;
            public string MobileLink;
            public string Link;
        }

        public Headline headline;

        public class Forecast
        {
            public string Date;
            public long EpochDate;

            public class Values
            {
                public double? Value;
                public string Unit;
                public int UnitType;
            }

            public class Temperature
            {
                public Values Minimum;
                public Values Maximum;
            }

            public Temperature temperature;

            public class Status
            {
                public int Icon;
                public string IconPhrase;
                public bool HasPrecipitation;
                public string ShortPhrase;
                public int CloudCover;

                public class Wind
                {
                    public Values Speed;

                    public class Direction
                    {
                        public double Degrees;
                        public string Localized;
                        public string English;
                    }

                    public Direction direction;
                }

                public Wind wind;
            }

            public Status Day;
            public Status Night;

            public string[] Sources;

            public string MobileLink;
            public string Link;
        }

        public Forecast[] DailyForecasts;

        public ResponseModel ToResponseModel(AccuWeatherLocationModel locModel, AccuWeatherCurrentModel current)
        {
            ResponseModel response = new ResponseModel()
            {
                Provider = "AccuWeather",
                CityName = locModel.EnglishName,
                Coords = new Coords(locModel.geoPosition.Latitude, locModel.geoPosition.Longitude),
                Country = locModel.country.ID,
                Now = new ResponseModel.Current()
                {
                    Date = current.EpochTime,
                    Temp = (float?)current.Temperature.Metric.value,
                    Cloudiness = current.CloudCover,
                    Humidity = current.RelativeHumidity,
                    WeatherDescription = current.WeatherText,
                    WeatherType = current.WeatherIcon.ToString(),
                    WindDeg = (float?)current.wind.direction.Degrees,
                    WindSpeed = (float?)current.wind.Speed.Metric.value
                },
                Forecasts = new ResponseModel.Forecast[DailyForecasts.Length]
            };

            for (int i = 0; i < DailyForecasts.Length; i++)
            {
                response.Forecasts[i] = new ResponseModel.Forecast
                {
                    Date = DailyForecasts[i].EpochDate,
                    TempMax = (float)DailyForecasts[i].temperature.Maximum.Value,
                    TempMin = (float)DailyForecasts[i].temperature.Minimum.Value,
                    //Humidity = DailyForecasts[i].,
                    WeatherType = DailyForecasts[i].Day.IconPhrase,
                    WeatherDescription = DailyForecasts[i].Day.ShortPhrase,
                    Cloudiness = DailyForecasts[i].Day.CloudCover,
                    WindSpeed = (float?)DailyForecasts[i].Day.wind.Speed.Value,
                    WindDeg = (float?)DailyForecasts[i].Day.wind.direction.Degrees
                };
            }

            return response;
        }
    }
}
