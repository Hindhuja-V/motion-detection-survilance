using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionDetectionSurvilance
{
    internal class MotionData
    {
        public int MotionValue { get; set; }

        public DateTime DateTime { get; set; }

    }

    internal class MotionDataCollection
    {
        private int maxCount;
        public List<MotionData> motionDatas = new List<MotionData>();
        private ObservableCollection<int> datas = new ObservableCollection<int>();

        public void AddMotion(int MotionValue)
        {
            motionDatas.Add(new MotionData() { DateTime = DateTime.Now, MotionValue = MotionValue });
            datas.Add(MotionValue);

            motionDatas.RemoveAt(0);
            datas.RemoveAt(0);
        }

        public MotionDataCollection(int MaxCount)
        {
            maxCount = MaxCount;
            for (int i = 0; i < maxCount; i++)
            {
                datas.Add(0);
                motionDatas.Add(new MotionData());
            }
        }


        public ObservableCollection<int> MotionValue
        {
            get
            {
                var arr = datas;
                return arr;
            }
        }
    }
}
