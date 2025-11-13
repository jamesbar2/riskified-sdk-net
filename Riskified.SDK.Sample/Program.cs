namespace Riskified.SDK.Sample
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {

            # region run all api endpoints
            if (args.Length > 0 && args[0] == "run_all")
                return OrderTransmissionExample.runAll();
            #endregion

            # region async example
            if (args.Length > 0 && args[0] == "async")
            {
                await OrderTransmissionExample.SendOrdersToRiskifiedAsyncExample();
                return 0;
            }
            #endregion

            # region dependency injection example
            if (args.Length > 0 && args[0] == "di")
            {
                await DependencyInjectionExample.RunDependencyInjectionExample();
                return 0;
            }
            if (args.Length > 0 && args[0] == "di-patterns")
            {
                DependencyInjectionExample.ShowDependencyInjectionPatterns();
                return 0;
            }
            #endregion

            # region notification example


            #endregion

            #region orders example

            OrderTransmissionExample.SendOrdersToRiskifiedExample();

            #endregion


            // make sure to shut down the notifications server when done

            return 0;

        }
    }
}
