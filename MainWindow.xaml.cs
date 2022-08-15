using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RFID_Based_BFCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "lJXucH2vqERMZXneGTKdBfpmRUEcEgDvMdzLpR6P",
            BasePath = "https://rfid-based-bfcs-default-rtdb.firebaseio.com/"
        };

        IFirebaseClient client;

        public List<UserInfo> userList { get; set; } = new List<UserInfo>();
        public List<TravelInfo> travelList { get; set; } = new List<TravelInfo>();
        public List<TravelInfo> travelListDisplay { get; set; } = new List<TravelInfo>();

        enum MessageType
        {
            ERROR,
            WARNING,
            INFO
        }
        public MainWindow()
        {
            InitializeComponent();
            client = new FirebaseClient(config);
            if(client != null)
            {
                getUserData();
                searchButton_Click(null, null);
            }
            else
            {
                popUpMessage("Something went wrong.", MessageType.ERROR);
            }
        }

        void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()+1).ToString();
        }

        void popUpMessage(string message, MessageType type){
            switch (type)
            {
                case MessageType.ERROR:
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case MessageType.WARNING:
                    MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                case MessageType.INFO:
                    MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                default: break;
            }
        }

        private async void addButton_Click(object sender, RoutedEventArgs e)
        {
            addButton.IsEnabled = false;
            UserInfo user = new UserInfo
            {
                UID = UIDTextbox.Text.Trim(),
                Address = AddressTextbox.Text.Trim(),
                BoardingStatus = "0", //0 -> on board, 1 -> drop off
                CardStatus = ((bool)CardStatActiveRadioButton.IsChecked) ? "1" : "0",//CardStatTextbox.Text.Trim(), //0 -> inactive, 1 -> active
                ContactNo = ContactNoTextbox.Text.Trim(),
                FirstName = FirstNameTextbox.Text.Trim(),
                MI = MITextbox.Text.Trim(),
                LastName = LastNameTextbox.Text.Trim(),
                Suffix = SuffixTextbox.Text.Trim(),
                Gender = ((bool)GenderMaleRadioButton.IsChecked) ? "M":"F",//GenderTextbox.Text.Trim(),
                LoadBal = LoadBalTextbox.Text.Trim()
            };
            try
            {
                SetResponse response = await client.SetAsync("User/" + UIDTextbox.Text.Trim(), user);
                clearInputField();
                popUpMessage("User successfully added.", MessageType.INFO);
                getUserData();
            }
            catch
            {
                popUpMessage("Something went wrong when adding user [" + UIDTextbox.Text.Trim() + "] to database.", MessageType.ERROR);
            }
            finally
            {
                addButton.IsEnabled = true;
            }
        }

        private async void updateButton_Click(object sender, RoutedEventArgs e)
        {
            updateButton.IsEnabled = false;
            deleteButton.IsEnabled = false;
            UserInfo selectedUser = (UserInfo)userListDataGrid.SelectedItem;
            Task<FirebaseResponse> delete = client.DeleteAsync("User/" + selectedUser.UID);
            await Task.WhenAll(delete);
            UserInfo user = new UserInfo
            {
                UID = UIDTextbox.Text.Trim(),
                Address = AddressTextbox.Text.Trim(),
                BoardingStatus = "0", //0 -> on board, 1 -> drop off
                CardStatus = ((bool)CardStatActiveRadioButton.IsChecked) ? "1" : "0",//CardStatTextbox.Text.Trim(), //0 -> inactive, 1 -> active
                ContactNo = ContactNoTextbox.Text.Trim(),
                FirstName = FirstNameTextbox.Text.Trim(),
                MI = MITextbox.Text.Trim(),
                LastName = LastNameTextbox.Text.Trim(),
                Suffix = SuffixTextbox.Text.Trim(),
                Gender = ((bool)GenderMaleRadioButton.IsChecked) ? "M" : "F",//GenderTextbox.Text.Trim(),
                LoadBal = LoadBalTextbox.Text.Trim()
            };
            SetResponse response = await client.SetAsync("User/" + UIDTextbox.Text.Trim(), user);
            popUpMessage("User successfully updated.", MessageType.INFO);
            clearInputField();
            getUserData();
        }

        private async void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            UserInfo selectedUser = (UserInfo)userListDataGrid.SelectedItem;
            Task<FirebaseResponse> delete = client.DeleteAsync("User/" + selectedUser.UID);
            await Task.WhenAll(delete);
            clearInputField();
            popUpMessage("User successfully deleted.", MessageType.INFO);
            getUserData();
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            searchButton.IsEnabled = false;
            FirebaseResponse response = await client.GetAsync("TravelHistory");
            Dictionary<string, Dictionary<string, TravelHistoryData>> result = response.ResultAs<Dictionary<string, Dictionary<string, TravelHistoryData>>>();
            travelList = new List<TravelInfo>();
            foreach (Dictionary<string, TravelHistoryData> travelInfo in result.Values)
            {
                for(int i = 0; i < travelInfo.Count; i++)
                {
                    UserInfo user = userList.Find(x => x.UID.Equals(travelInfo.ElementAt(i).Value.UID)) ?? new UserInfo();
                    string name = user.FirstName + " " + user.MI + ". " + user.LastName + ", " + user.Suffix;
                    travelList.Add(new TravelInfo
                    {
                        DateTime = travelInfo.ElementAt(i).Value.DateTime,
                        UID = travelInfo.ElementAt(i).Value.UID,
                        Origin = travelInfo.ElementAt(i).Value.Origin,
                        Destination = travelInfo.ElementAt(i).Value.Destination,
                        Fare = travelInfo.ElementAt(i).Value.Fare,
                        Name = name
                    });
                }
            }
            travelListDisplay = travelList;
            string filter = searchTextbox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(filter))
            {
                travelListDisplay = travelList.FindAll(x => (!string.IsNullOrEmpty(x.UID) && x.UID.Contains(filter))
                || (!string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(filter)) || (!string.IsNullOrEmpty(x.DateTime) && x.DateTime.ToLower().Contains(filter)));
            }
            travelListDataGrid.ItemsSource = travelListDisplay;
            searchButton.IsEnabled = true;
        }

        private void UserRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            updateButton.IsEnabled = true;
            deleteButton.IsEnabled = true;
            addButton.IsEnabled = false;
            reflectDataToInputField();
        }

        private async void getUserData()
        {
            FirebaseResponse response = await client.GetAsync("User");
            Dictionary<string, UserInfo> result = response.ResultAs<Dictionary<string, UserInfo>>();
            userList = new List<UserInfo>();
            foreach(UserInfo userInfo in result.Values)
            {
                userList.Add(userInfo);
            }
            userListDataGrid.ItemsSource = userList;
        }

        private void reflectDataToInputField()
        {
            UserInfo selectedUser = (UserInfo)userListDataGrid.SelectedItem;
            UIDTextbox.Text = selectedUser.UID;
            AddressTextbox.Text = selectedUser.Address;
            //CardStatTextbox.Text = selectedUser.CardStatus;
            if (selectedUser.CardStatus.Equals("1"))
            {
                CardStatActiveRadioButton.IsChecked = true;
            }
            else
            {
                CardStatInactiveRadioButton.IsChecked = true;
            }
            ContactNoTextbox.Text = selectedUser.ContactNo;
            FirstNameTextbox.Text = selectedUser.FirstName;
            MITextbox.Text = selectedUser.MI;
            LastNameTextbox.Text = selectedUser.LastName;
            SuffixTextbox.Text = selectedUser.Suffix;
            //GenderTextbox.Text = selectedUser.Gender;
            if (selectedUser.Gender.Equals("M"))
            {
                GenderMaleRadioButton.IsChecked = true;
            }
            else
            {
                GenderFemaleRadioButton.IsChecked = true;
            }
            LoadBalTextbox.Text = selectedUser.LoadBal;
        }

        private void clearInputField()
        {
            UIDTextbox.Text = "";
            AddressTextbox.Text = "";
            //CardStatTextbox.Text = "";
            CardStatActiveRadioButton.IsChecked = true;
            ContactNoTextbox.Text = "";
            FirstNameTextbox.Text = "";
            MITextbox.Text = "";
            LastNameTextbox.Text = "";
            SuffixTextbox.Text = "";
            //GenderTextbox.Text = "";
            GenderMaleRadioButton.IsChecked = true;
            LoadBalTextbox.Text = "";

            addButton.IsEnabled = true;
            updateButton.IsEnabled = false;
            deleteButton.IsEnabled = false;
        }
    }
    public class UserInfo
    {
        public string? UID { get; set; }
        public string? Address { get; set; }
        public string? BoardingStatus { get; set; }
        public string? CardStatus { get; set; }
        public string? ContactNo { get; set; }
        public string? FirstName { get; set; }
        public string? MI { get; set; }
        public string? LastName { get; set; }
        public string? Suffix { get; set; }
        public string? Gender { get; set; }
        public string? LoadBal { get; set; }
    }
    public class TravelInfo
    {
        public string? DateTime { get; set; }
        public string? UID { get; set; }
        public string? Name { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public string? Fare { get; set; }
    }
    public class TravelHistoryData
    {
        public string? DateTime { get; set; }
        public string? UID { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public string? Fare { get; set; }
    }
}
