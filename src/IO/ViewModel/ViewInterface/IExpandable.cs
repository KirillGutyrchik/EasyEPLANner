﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.ViewModel
{
    /// <summary>
    /// Элемент имеет дочерние элементы и 
    /// может быть открыт(развернут) на представлении
    /// </summary>
    public interface IExpandable
    {
        /// <summary>
        /// Дочерние элементы
        /// </summary>
        public IEnumerable<IViewItem> Items { get; }
    }
}
