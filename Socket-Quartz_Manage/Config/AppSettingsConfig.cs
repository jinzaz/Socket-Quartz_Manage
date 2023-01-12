namespace Socket_Quartz_Manage.Config
{
    public class AppSettingsConfig
    {
        public string SocketBindIP { get; set; }
        public int SocketBindPort { get; set; }
        public string MqttPushTopic { get; set; }
        public string MqttClientId { get; set; }
        public string MqttServer { get; set; }
        public int MqttPort { get; set; }
        public string MqttUserName { get; set; }
        public string MqttPassword { get; set; }
    }
}
