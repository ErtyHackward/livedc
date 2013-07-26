using System;
using System.Collections.Generic;

namespace LiveDc.Helpers
{
    public static class TimeFormatHelper
    {
        public static string Format(DateTime time)
        {
            var diff = DateTime.Now - time;

            if (diff.TotalMinutes < 0)
            {
                return "� ������� o_O";
            }

            if (diff.TotalMinutes < 1)
            {
                return "������ ���";
            }

            if (diff.TotalMinutes < 60)
            {
                return FormatWord((int)diff.TotalMinutes, "������", "������", "�����") + " �����";
            }

            if (diff.TotalHours < 6)
            {
                return FormatWord((int)diff.TotalHours, "���", "����", "�����") + " �����";
            }

            var pozavchera = DateTime.Now.Date.AddDays(-2);
            var vchera = DateTime.Now.Date.AddDays(-1);
            var today = DateTime.Now.Date;

            if (time >= today)
            {
                return "������� " + GetTimeOfDayName(time.TimeOfDay);
            }
            if (time >= vchera)
            {
                return "����� " + GetTimeOfDayName(time.TimeOfDay);
            }
            if (time >= pozavchera)
            {
                return "��������� " + GetTimeOfDayName(time.TimeOfDay);
            }

            if (diff.TotalDays < 7)
                return FormatWord((int)diff.TotalDays, "����", "���", "����") + " �����";

            return time.ToLongDateString();
        }

        private static List<KeyValuePair<TimeSpan, string>> _dayTime = new List<KeyValuePair<TimeSpan, string>>();

        static TimeFormatHelper()
        {
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("00:00"), "�������"));
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("01:00"), "�����"));
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("06:00"), "�����"));
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("12:00"), "� ����"));
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("14:00"), "����"));
            _dayTime.Add(new KeyValuePair<TimeSpan, string>(TimeSpan.Parse("18:00"), "�������"));
        }

        public static string GetTimeOfDayName(TimeSpan time)
        {
            for (int i = 0; i < _dayTime.Count; i++)
            {
                if (time < _dayTime[i].Key)
                    return _dayTime[i - 1].Value;
            }
            return _dayTime[_dayTime.Count - 1].Value;
        }

        // 1 ��������, 2 ���������, 5 ����������
        public static string FormatWord(int value,
                                string nominative, //������������ �����
                                string genitiveSingular,//����������� �����, ������������ �����
                                string genitivePlural) //����������� �����, ������������� �����
        {

            int[] formsTable = { 2, 0, 1, 1, 1, 2, 2, 2, 2, 2 };

            value = Math.Abs(value);
            int res = formsTable[((((value % 100) / 10) != 1) ? 1 : 0) * (value % 10)];
            switch (res)
            {
                case 0:
                    return string.Format("{0} {1}", value, nominative);
                case 1:
                    return string.Format("{0} {1}", value, genitiveSingular);
                default:
                    return string.Format("{0} {1}", value, genitivePlural);
            }
        }

        public static string FormatInterval(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
            {
                return string.Format("{0} ms", Math.Round(timeSpan.TotalMilliseconds));
            }

            if (timeSpan.TotalSeconds < 10)
            {
                return string.Format("{0} s", Math.Round(timeSpan.TotalSeconds, 2));
            }

            if (timeSpan.TotalMinutes < 0)
            {
                return string.Format("{0} s", Math.Round(timeSpan.TotalSeconds));
            }

            if (timeSpan.TotalHours < 0)
            {
                return string.Format("{0} m", Math.Round(timeSpan.TotalMinutes));
            }

            return timeSpan.ToString();
        }
    }
}
