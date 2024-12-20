﻿using Eplan.EplApi.DataModel;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace EplanDevice
{
    public partial class IODevice
    {
        /// <summary>
        /// Параметр времени выполнения устройства.
        /// </summary>
        public class RuntimeParameter
        {
            /// <summary>
            /// Номер клапана на пневмоострове
            /// </summary>
            public static readonly RuntimeParameter R_VTUG_NUMBER = new RuntimeParameter(nameof(R_VTUG_NUMBER));

            /// <summary>
            /// Размер области клапана для пневмоострова
            /// </summary>
            public static readonly RuntimeParameter R_VTUG_SIZE = new RuntimeParameter(nameof(R_VTUG_SIZE));

            /// <summary>
            /// Номер клапана в AS-i.
            /// </summary>
            public static readonly RuntimeParameter R_AS_NUMBER = new RuntimeParameter(nameof(R_AS_NUMBER));

            /// <summary>
            /// Тип красного сигнала устройства при подаче на него сигнала DO. 
            /// (Постоянный или мигающий). 1 - мигающий, 0 - постоянный.
            /// </summary>
            public static readonly RuntimeParameter R_CONST_RED = new RuntimeParameter(nameof(R_CONST_RED));

            /// <summary>
            /// Номер клеммы пневмоострова для сигнала "Открыть"
            /// </summary>
            public static readonly RuntimeParameter R_ID_ON = new RuntimeParameter(nameof(R_ID_ON));

            /// <summary>
            /// Номер клеммы пневмоострова для сигнала "Открыть верхнее седло"
            /// </summary>
            public static readonly RuntimeParameter R_ID_UPPER_SEAT = new RuntimeParameter(nameof(R_ID_UPPER_SEAT));

            /// <summary>
            /// Номер клеммы пневмоострова для сигнала "Открыть нижнее седло"
            /// </summary>
            public static readonly RuntimeParameter R_ID_LOWER_SEAT = new RuntimeParameter(nameof(R_ID_LOWER_SEAT));

            /// <summary>
            /// Смещение адресного пространства для клапанов 
            /// <see cref="DeviceSubType.V_IOLINK_MIXPROOF"/>, <see cref="DeviceSubType.V_IOLINK_DO1_DI2"/>
            /// привязанных к модулю WAGO.750-657
            /// </summary>
            public static readonly RuntimeParameter R_EXTRA_OFFSET = new RuntimeParameter(nameof(R_EXTRA_OFFSET), true);


            private RuntimeParameter(string name, bool auto = false)
            {
                Name = name;
                AutoGenerated = auto;
            }


            protected static readonly Lazy<Dictionary<string, RuntimeParameter>> AllParameters = InitParameters();
            private static Lazy<Dictionary<string, RuntimeParameter>> InitParameters()
            {
                return new Lazy<Dictionary<string, RuntimeParameter>>(() =>
                {
                    var parameters = typeof(RuntimeParameter)
                        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(x => x.FieldType == typeof(RuntimeParameter))
                        .Select(x => x.GetValue(null))
                        .OfType<RuntimeParameter>()
                        .ToDictionary(x => x.Name, x => x);
                    return parameters;
                });
            }


            /// <summary>
            /// Явное преобразование строки в параметр
            /// </summary>
            public static explicit operator RuntimeParameter(string parameterName)
            {
                if (AllParameters.Value.TryGetValue(parameterName, out var parameter))
                {
                    return parameter;
                }
                else
                {
                    return new RuntimeParameter(parameterName);
                }
            }


            /// <summary>
            /// Неявное преобразование параметра в строку
            /// </summary>
            public static implicit operator string(RuntimeParameter parameterType)
            {
                return parameterType.Name;
            }


            /// <summary>
            /// Название параметра
            /// </summary>
            public string Name { get; private set; }


            /// <summary>
            /// Параметр настраивается автоматически
            /// </summary>
            /// <remarks>
            /// Если свойство не установлено то в логах не отображается ошибки
            /// </remarks>
            public bool AutoGenerated { get; private set; }
        }
    }
}
