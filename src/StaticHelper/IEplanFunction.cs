﻿using EasyEPlanner.Extensions;
using Eplan.EplApi.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticHelper
{
    /// <summary>
    /// Обертка для <see cref="Function">функции</see> на ФСА
    /// </summary>
    public interface IEplanFunction
    {
        /// <summary>
        /// Поле "Начальный адрес карты ПЛК"
        /// </summary>
        string IP { get; set; }

        /// <summary>
        /// Доп.поле [<paramref name="propertyIndex"/>]
        /// </summary>
        /// <param name="propertyIndex">Номер доп. поля</param>
        /// <returns>Содержимое доп. поля</returns>
        string GetSupplemenataryField(int propertyIndex);

        /// <summary>
        /// Сетевой шлюз
        /// </summary>
        string Gateway { get; set; }

        string SubnetMask { get; set; }

        int ClampNumber { get; }

        string VisibleName { get; }

        string Name { get; }

        IEnumerable<IEplanFunction> SubFunctions { get; }

        string FunctionalText { get; set; }

        bool PlacedOnCircuit { get; }

        bool IsMainFunction {  get; }
    }

    /// <summary>
    /// <inheritdoc cref="IEplanFunction"/>
    /// </summary>
    /// <param name="function"></param>
    public class EplanFunction(Function function) : IEplanFunction
    {
        public Function Function => function;

        public string IP 
        {
            get => function.Properties.FUNC_PLCGROUP_STARTADDRESS.GetString();
            set => function.Properties.FUNC_PLCGROUP_STARTADDRESS = value;
        }

        public string SubnetMask 
        { 
            get => function.Properties.FUNC_PLC_SUBNETMASK; 
            set => function.Properties.FUNC_PLC_SUBNETMASK = value;
        }

        public string Gateway 
        { 
            get => GetSupplemenataryField(15); 
            set => SetSupplementaryField(15, value); 
        }

        public int ClampNumber => int.TryParse(
            function.Properties.FUNC_ADDITIONALIDENTIFYINGNAMEPART.GetString(),
            out int clamp) ? clamp : -1;

        public string VisibleName => function.VisibleName;

        public IEnumerable<IEplanFunction> SubFunctions => function.SubFunctions.Select(f => new EplanFunction(f));

        public string FunctionalText 
        { 
            get => function.GetFunctionalText();
            set => function.Properties.FUNC_TEXT = value;
        }

        public string Name => function.Name;

        public bool PlacedOnCircuit => function.Page.PageType is DocumentTypeManager.DocumentType.Circuit;

        public string GetSupplemenataryField(int propertyIndex)
            => function.Properties.FUNC_SUPPLEMENTARYFIELD[propertyIndex].GetString();

        public void SetSupplementaryField(int propertyIndex, string value)
        {
            function.Properties.FUNC_SUPPLEMENTARYFIELD[propertyIndex] = value;
        }

        public bool IsMainFunction => function.IsMainFunction;

        //public bool IsClamp => function.Page.PageType is DocumentTypeManager.DocumentType.;
    }
}
