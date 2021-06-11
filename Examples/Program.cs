using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Extensions.Logging.ElapsedTime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            

            var logger = LoggerFactory.Create(builder => { builder.AddConsole(); }).CreateLogger<Program>();
            logger.LogInformation("Hello Loggers!");


            var example = new Example();
            example.Operation1();
            example.Operation2();
            example.Operation3();
            example.Operation4();
            example.Operation5();
        }
    }

    public class Example
    {
        private readonly ILogger _logger;

        public Example()
        {
            _logger = LoggerFactory.Create(builder => { builder.AddConsole(); }).CreateLogger<Example>();
        }

        public void Operation1()
        {
            _logger.LogInformation("Operation1: StandardLog");
        }

        public void Operation2()
        {
            using (_logger.TimeOperation("Operation2"))
            {
                Thread.Sleep(1000);
            }
        }

        public void Operation3()
        {
            using var operation = _logger.BeginOperation("Operation3");
            Thread.Sleep(1000);
            operation.Complete(true);
        }

        public void Operation4()
        {
            using var operation = _logger.BeginOperation("Operation4");
            Thread.Sleep(1000);
            operation.Complete(false);
        }

        public void Operation5()
        {
            using var operation = _logger.BeginOperation("Operation5");
            Thread.Sleep(1000);
            operation.Abandon();
        }
    }
}
