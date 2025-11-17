
using System.Diagnostics;
using Mono.Cecil;
using UnityEngine;

namespace DynamicMeshCutter
{
    public class PlaneBehaviour : CutterBehaviour
    {
        public float DebugPlaneLength = 2;
        public float cutRadius = 0.6f;
        public LayerMask cutLayer;

        public void Cut()
        {
            /*var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (!root.activeInHierarchy)
                    continue;
                var targets = root.GetComponentsInChildren<MeshTarget>();
                foreach (var target in targets)
                {
                    Cut(target, transform.position, transform.forward, null, OnCreated);
                }
            }*/

            //UnityEngine.Debug.Log("cirr");

            Collider[] hits = Physics.OverlapBox(transform.position, transform.lossyScale/2, transform.rotation, cutLayer);
            DebugDrawBox(transform.position, transform.lossyScale/2, transform.rotation, Color.green);
            foreach (var h in hits)
            {
                //var target = h.GetComponentInParent<MeshTarget>();
                //var target = h.GetComponentInChildren<MeshTarget>();

                UnityEngine.Debug.Log($"Hit collider: {h.name} (root: {h.transform.root.name})");

                // 1) Tenta o método simples e rápido
                /*MeshTarget target = h.GetComponentInParent<MeshTarget>();

                // 2) Se não achou, tenta procurar nos filhos do root (caso o MeshTarget esteja abaixo do root)
                if (target == null)
                    target = h.transform.root.GetComponentInChildren<MeshTarget>(true);

                // 3) fallback: sobe manualmente na hierarquia (funciona mesmo com objetos inativos)
                if (target == null)
                {
                    Transform t = h.transform;
                    while (t != null && target == null)
                    {
                        target = t.GetComponent<MeshTarget>();
                        t = t.parent;
                    }
                }*/
                //PARA FUNCIONAR O COLLIDER DEVE ESTAR NA MESH!!!!
                MeshTarget target = h.GetComponent<MeshTarget>();

                if (target == null)
                {
                    UnityEngine.Debug.Log($"No MeshTarget found for collider {h.name}");
                    continue;
                }

                //Se for inimigo chama a função de morrer
                if (h.GetComponentInParent<EnemyAI>() != null)
                {
                    //UnityEngine.Debug.Log("matou o veio");
                    h.GetComponentInParent<EnemyAI>().die();
                }

                //DetachLimbs(target.gameObject);
                Cut(target, transform.position, transform.forward, null, OnCreated);
            }
        }
        
        /*public void Cut(Collision col)
        {

            DebugDrawBox(transform.position, transform.lossyScale, transform.rotation, Color.green);

            var h = col.gameObject;
            //var target = h.GetComponentInParent<MeshTarget>();
            //var target = h.GetComponentInChildren<MeshTarget>();

            UnityEngine.Debug.Log($"Hit collider: {h.name} (root: {h.transform.root.name})");

            // 1) Tenta o método simples e rápido
            MeshTarget target = h.GetComponentInParent<MeshTarget>();

            // 2) Se não achou, tenta procurar nos filhos do root (caso o MeshTarget esteja abaixo do root)
            if (target == null)
                target = h.transform.root.GetComponentInChildren<MeshTarget>(true);

            // 3) fallback: sobe manualmente na hierarquia (funciona mesmo com objetos inativos)
            if (target == null)
            {
                Transform t = h.transform;
                while (t != null && target == null)
                {
                    target = t.GetComponent<MeshTarget>();
                    t = t.parent;
                }
            }

            if (target == null)
            {
                UnityEngine.Debug.Log($"No MeshTarget found for collider {h.name}");
                return;
            }

            //UnityEngine.Debug.Log(target.name);
            //DetachLimbs(target.gameObject);
            Cut(target, transform.position, transform.forward, null, OnCreated);
        }*/

        void OnCreated(Info info, MeshCreationData cData)
        //depois de cortar aqui que cria o corpo morto
        {
            MeshCreation.TranslateCreatedObjects(info, cData.CreatedObjects, cData.CreatedTargets, Separation);
            /*foreach (var go in cData.CreatedObjects)
            {
                if (go == null) continue;
                foreach (Transform t in go.transform) t.gameObject.layer = LayerMask.NameToLayer("Corte");
            }*/
        }

        void DetachLimbs(GameObject targetRoot)
        {
            var limbs = targetRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in limbs)
            {
                if (t.gameObject == targetRoot) continue;
                
                //Seta como corte
                //t.gameObject.layer = LayerMask.NameToLayer("Corte");

                // unparentar e habilitar física para "dropar"
                Transform limb = t;
                limb.SetParent(null, true); // worldPositionStays = true

                // adiciona Rigidbody/collider se não tiver
                if (limb.GetComponent<Rigidbody>() == null)
                {
                    var rb = limb.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }

                /*if (limb.GetComponent<Collider>() == null)
                {
                    // tenta adicionar um BoxCollider simples (ajuste conforme necessário)
                    var bc = limb.gameObject.AddComponent<BoxCollider>();
                    // opcional: ajustar bc.center/size manualmente no inspector
                }*/
                
            }
        }


        void DebugDrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color)
        {

            // desenha o box da lâmina
            Vector3[] corners = new Vector3[8];
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            for (int i = 0; i < 8; i++)
            {
                corners[i] = center +
                    right * ((i & 1) == 0 ? -halfExtents.x : halfExtents.x) +
                    up * ((i & 2) == 0 ? -halfExtents.y : halfExtents.y) +
                    forward * ((i & 4) == 0 ? -halfExtents.z : halfExtents.z);
            }
            
            UnityEngine.Debug.DrawLine(corners[0], corners[1], color, 5.0f);
            UnityEngine.Debug.DrawLine(corners[1], corners[3], color, 5.0f);
            UnityEngine.Debug.DrawLine(corners[3], corners[2], color, 5.0f);
            UnityEngine.Debug.DrawLine(corners[2], corners[0], color, 5.0f);

            UnityEngine.Debug.DrawLine(corners[4], corners[5], color);
            UnityEngine.Debug.DrawLine(corners[5], corners[7], color);
            UnityEngine.Debug.DrawLine(corners[7], corners[6], color);
            UnityEngine.Debug.DrawLine(corners[6], corners[4], color);

            UnityEngine.Debug.DrawLine(corners[0], corners[4], color);
            UnityEngine.Debug.DrawLine(corners[1], corners[5], color);
            UnityEngine.Debug.DrawLine(corners[2], corners[6], color);
            UnityEngine.Debug.DrawLine(corners[3], corners[7], color);
        }

    }
}