using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criação, reaproveitamento e reciclagem de GameObjects na memória.
/// Evita o uso excessivo de Instantiate e Destroy durante a partida.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        [Tooltip("Nome de identificação da tag do objeto.")]
        public string tag;

        [Tooltip("O Prefab do objeto a ser reaproveitado.")]
        public GameObject prefab;

        [Tooltip("Quantidade inicial de objetos a serem pré-carregados na memória.")]
        public int size = 5;
    }

    [Header("Configurações das Piscinas (Pools)")]
    [Tooltip("Lista de grupos de objetos que serão gerenciados pela piscina.")]
    [SerializeField] private List<Pool> pools;

    // Dicionário interno para armazenar as filas de objetos inativos por tag
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // Garante que só exista uma instância do ObjectPooler na cena
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializamos o dicionário no Awake para garantir que esteja pronto antes do Start de outros scripts
        InitializePools();
    }

    /// <summary>
    /// Instancia previamente todos os objetos configurados no Inspector.
    /// </summary>
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                if (pool.prefab == null)
                {
                    Debug.LogError($"O Prefab da tag '{pool.tag}' está faltando no Inspector do ObjectPooler!");
                    continue;
                }

                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    /// <summary>
    /// Retorna um objeto inativo da piscina e o posiciona na cena de forma segura.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        // 1. Validação se o dicionário foi inicializado
        if (poolDictionary == null)
        {
            Debug.LogError("O poolDictionary não foi inicializado. Verifique se o ObjectPooler está ativo na cena.");
            return null;
        }

        // 2. Validação se a tag solicitada existe cadastrada
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"A tag '{tag}' NÃO foi encontrada no ObjectPooler! Verifique se digitou o nome exatamente igual no ChunkSpawner e no ObjectPooler.");
            return null;
        }

        // 3. Obtém a fila correspondente à tag
        Queue<GameObject> objectQueue = poolDictionary[tag];

        if (objectQueue.Count == 0)
        {
            Debug.LogWarning($"A piscina da tag '{tag}' está vazia! Considere aumentar o 'Size' no Inspector.");
            return null;
        }

        // Retira o primeiro objeto da fila
        GameObject objectToSpawn = objectQueue.Dequeue();

        // Posiciona e ativa o objeto
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        // Devolve o objeto para o final da fila para ser reusado futuramente
        objectQueue.Enqueue(objectToSpawn);

        return objectToSpawn;
    }
}