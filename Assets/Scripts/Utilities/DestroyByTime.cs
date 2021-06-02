using UnityEngine;

namespace Utilities
{
    public class DestroyByTime : MonoBehaviour
    {

        private float destructionTime = 5;

        // Start is called before the first frame update

        // Update is called once per frame
        void Update()
        {
            //if destructionTime is still greater than 0
            if (destructionTime > 0)
            {
                //continue the countdown
                destructionTime -= Time.deltaTime;
            }
            else
            {
                //yeet this after countdown expires
                Destroy(this.gameObject);
            }
        }
    }
}
