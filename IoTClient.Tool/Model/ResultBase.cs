using System;
using System.Collections.Generic;

namespace IoTClient.Tool.Model
{
    public class ResultBase<T>
    { 
        public bool IsSuccess { get; }
        public int Code { get; set; }
        public string ErrorMsg { get; set; } 
        public List<string> ErrorList { get; set; }
        public T Data { get; set; } 
    }
}
