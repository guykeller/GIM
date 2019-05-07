using System;
using System.Collections.Generic;


namespace CounterApp
{
    public class MainViewModel
    {
        public List<CounterModel> Counters { get; }

        public MainViewModel()
        {
            Counters = new List<CounterModel>
            {
                new CounterModel(1)
            };
        }


    }
}