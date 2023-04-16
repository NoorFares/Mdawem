using Firebase.Auth;
using Firebase.Database;

using Firebase.Database.Query;

using MdawemApp.Models;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace MdawemApp.Helper
{
    public class FirebaseHelper
    {
        string webAPIKey = "AIzaSyDuxpf83oL4rNwmPBV06DEid9xUWPNyOWU";
        FirebaseAuthProvider authProvider;
        FirebaseClient client = new FirebaseClient(
        "https://mdawemt-default-rtdb.firebaseio.com/"
    );
        public FirebaseHelper()
        {
            authProvider = new FirebaseAuthProvider(new FirebaseConfig(webAPIKey));
        }


        public async Task<string> Login(string email, string password)
        {
            var token = await authProvider.SignInWithEmailAndPasswordAsync(email, password);
            if (!string.IsNullOrEmpty(token.FirebaseToken))
            {
                Application.Current.Properties["UID"] = token.User.LocalId;
                return token.FirebaseToken;
            }
            {
                return "";
            }
        }

        public void SignOut()
        {
            Application.Current.Properties.Remove("emailtxt");
            Application.Current.Properties.Remove("passwordtxt");
            Application.Current.Properties.Remove("UID");

        }
        public async Task<bool> Register(string email, string password)
        {
            var token = await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
            if (!string.IsNullOrEmpty(token.FirebaseToken))
            {
                return true;
            }
            return false;
        }


        public async Task<bool> CheckIn(string userId, double latitude, double longitude)
        {
            try
            {
                DateTime currentDate = DateTime.Now;

                string formattedDateWithOutDay = currentDate.ToString("yyyy/MM");
                string formattedDate = currentDate.ToString("yyyy/MM/dd");

                var AttendanceData = new
                {
                    Date = formattedDate,
                    Latitude = latitude,
                    Longitude = longitude,
                    TimeIn = currentDate.ToString("hh:mm:ss tt"),
                    TimeOut = ""
                };

                var result = await client
                .Child("users")
                .Child(userId)
                .Child("Attendance")
                .Child(formattedDateWithOutDay)
                .PostAsync(AttendanceData);
                Preferences.Set("attendanceKey", result.Key);

                return true;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");

                return false;
            }
        }
        public async Task<bool> CheckOut(string userId)
        {
            try
            {
                DateTime currentDate = DateTime.Now;
                string formattedDate = currentDate.ToString("yyyy/MM/dd");
                string formattedDateWithOutDay = currentDate.ToString("yyyy/MM");
                string attendanceKey = Preferences.Get("attendanceKey", string.Empty);

                var attendanceSnapshot = await client
                     .Child("users")
                     .Child(userId)
                     .Child("Attendance")
                     .Child(formattedDateWithOutDay)
                     .Child(attendanceKey)
                     .OnceSingleAsync<Dictionary<string, object>>();

                if (attendanceSnapshot != null)
                {

                    var attendanceData = attendanceSnapshot;
                    attendanceData["TimeOut"] = currentDate.ToString("hh:mm:ss tt");

                    await client
                        .Child("users")
                        .Child(userId)
                        .Child("Attendance")
                        .Child(formattedDateWithOutDay)
                        .Child(attendanceKey)
                        .PutAsync(attendanceData);
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");

                return false;
            }

        }
        public async Task<List<Attendance>> GetAttendance(string userId, string year, string month)
        {

            string path = $"users/{userId}/locations/{year}/{month}";

            var dataSnapshot = await client.Child(path).OnceAsync<object>();

            if (!dataSnapshot.Any())
            {
                return null;
            }

            var Attendances = new List<Attendance>();

            foreach (var childSnapshot in dataSnapshot)
            {
                var value = childSnapshot.Object;
                var valueJson = value.ToString();
                var Attend = JsonConvert.DeserializeObject<Attendance>(valueJson);

                var attendanceViewModel = new Attendance
                {
                    Date = Attend.Date,
                    TimeIn = Attend.TimeIn,
                    TimeOut = Attend.TimeOut
                };

                Attendances.Add(attendanceViewModel);
            }
            return Attendances;
        }
        public async Task<List<Attendance>> GetEmployeesLocations(string year, string month)
        {
            string attendancePath = $"attendance/{year}/{month}";
            var dataSnapshot = await client.Child("users").OnceAsync<object>();

            var locations = new List<Attendance>();

            foreach (var childSnapshot in dataSnapshot)
            {
                var userId = childSnapshot.Key;
                var attendanceSnapshot = await client.Child($"users/{userId}/{attendancePath}").OnceAsync<object>();

                foreach (var attendanceChildSnapshot in attendanceSnapshot)
                {
                    var value = attendanceChildSnapshot.Object;
                    var locationJson = value.ToString();
                    var location = JsonConvert.DeserializeObject<Attendance>(locationJson);


                    var locationViewModel = new Attendance
                    {
                        Date = location.Date,
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                    };
                    DateTime now = DateTime.Now;
                    CultureInfo culture = new CultureInfo("en-US");
                    string formattedDate = now.ToString("yyyy/MM/dd", culture);


                    if (locationViewModel.Date == formattedDate)
                    {
                        locations.Add(locationViewModel);
                    }

                }
            }

            return locations;
        }

    }

}