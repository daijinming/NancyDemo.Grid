using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAO
{   
    /// <summary>
    /// 分页数据集合，用于后端返回分页好的集合及前端视图分页控件绑定
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedList<T>
    {
        private T[] _items = null;

        public T[] Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public PagedList()
        { }

        

        public PagedList(T[] items, int pageIndex, int pageSize, int totalItemCount)
        {
            this.Items = items;

            TotalItemCount = totalItemCount;
            CurrentPageIndex = pageIndex;
            PageSize = pageSize;
        }

        public int CurrentPageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalItemCount { get; set; }

    }
     
   
}
