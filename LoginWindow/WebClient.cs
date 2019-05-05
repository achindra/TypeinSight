using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LoginWindow
{
   public class inputData
   {
      public string User;
      public string Machine;
      public string strData;
      public int strTime;
      public int grams;
   }

   public class conviction
   {
      public string User;
      public string Machine;
      public string strData;
      public int strTime;
      public int grams;
      [JsonProperty("Scored Labels")]
      public string Label;
      [JsonProperty("Scored Probabilities")]
      public double Probability;
   }

   public class MLWebClient : DispatcherObject
   {
      public static async void InvokeRequestResponseService(string strData, uint strTime, uint grams)
      {
         using (var client = new HttpClient())
         {
            var scoreRequest = new
            {
               Inputs = new Dictionary<string, List<Dictionary<string, string>>>() {
                        {
                            "input1",
                            new List<Dictionary<string, string>>(){new Dictionary<string, string>(){
                                            {
                                                "User", "Login"
                                            },
                                            {
                                                "Machine", Environment.MachineName
                                            },
                                            {
                                                "strData", strData
                                            },
                                            {
                                                "strTime", strTime.ToString()
                                            },
                                            {
                                                "grams", grams.ToString()
                                            },
                                }
                            }
                        },
                    },
               GlobalParameters = new Dictionary<string, string>()
               {
               }
            };

            const string apiKey = "44+7ZBdnwW2du0KbhB7MPMprCPZKOFa3hCpz1WoP995j7oCXsW6Wo4fQIL+/OQ2t/teRzcCICaE8bPMbfdjAqA=="; // Replace this with the API key for the web service
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/84e7e4140592498c9655867a5ed2cfa8/services/516f66cd1ef24552a78cc21d3d3d8580/execute?api-version=2.0&format=swagger");

            // WARNING: The 'await' statement below can result in a deadlock
            // if you are calling this code from the UI thread of an ASP.Net application.
            // One way to address this would be to call ConfigureAwait(false)
            // so that the execution does not attempt to resume on the original context.
            // For instance, replace code such as:
            //      result = await DoSomeTask()
            // with the following:
            //      result = await DoSomeTask().ConfigureAwait(false)

            HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

            if (response.IsSuccessStatusCode)
            {
               string result = await response.Content.ReadAsStringAsync();
               Console.WriteLine("Result: {0}", result);

               Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
               {
                  var mainWindow = (Application.Current.MainWindow as MainWindow);
                  mainWindow.txtBlock.Text = result + "\n" + mainWindow.txtBlock.Text;
               }));
            }
            else
            {
               Console.WriteLine(string.Format("The request failed with status code: {0}", response.StatusCode));

               // Print the headers - they include the requert ID and the timestamp,
               // which are useful for debugging the failure
               Console.WriteLine(response.Headers.ToString());

               string responseContent = await response.Content.ReadAsStringAsync();
               Console.WriteLine(responseContent);
            }
         }
      }


   }
}
