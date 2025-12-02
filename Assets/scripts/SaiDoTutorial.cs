using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicMeshCutter
{
    public class SaiDoTutorial : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                Iniciar();
            }
        }

        public void Iniciar()
        {
            SceneManager.LoadScene("MapaPrincipal");
        }
    }

}
