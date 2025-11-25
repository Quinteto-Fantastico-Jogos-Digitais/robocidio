using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicMeshCutter
{
    public class ReloadDemo : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                reload();
            }
        }

        public void reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
        }
    }

}
