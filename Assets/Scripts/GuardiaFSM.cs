using UnityEngine;
using UnityEngine.AI;

public class GuardiaFSM : MonoBehaviour
{
    public enum Estado
    {
        Patrullar,
        Perseguir,
        Atacar,
        Buscar,
        Huir
    }

    public Estado estadoActual = Estado.Patrullar;

    [Header("Objetivos")]
    public Transform jugador;
    public Transform[] puntosPatrulla;

    [Header("IA")]
    public float distanciaVision = 10f;
    public float distanciaAtaque = 2f;
    public float salud = 100f;
    public float saludMinimaParaHuir = 25f;

    [Header("Búsqueda")]
    public float tiempoBusqueda = 5f;
    public float distanciaLlegadaBusqueda = 0.5f;

    [Header("Ataque")]
    public float tiempoEntreAtaques = 1.5f;
    public float dañoAtaque = 10f;

    [Header("Visión")]
    public LayerMask capasObstaculos;

    private NavMeshAgent agente;

    private int puntoActual = 0;
    private float temporizadorAtaque = 0f;

    private Vector3 ultimoPuntoVisto;
    private float temporizadorBusqueda = 0f;


    // ============================================================
    // INICIO
    // ============================================================

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        if (agente == null)
        {
            Debug.LogError(
                "El Enemy necesita un NavMeshAgent."
            );

            enabled = false;
            return;
        }

        if (!agente.isOnNavMesh)
        {
            Debug.LogError(
                "El Enemy no está sobre un NavMesh."
            );

            return;
        }

        if (puntosPatrulla != null &&
            puntosPatrulla.Length > 0)
        {
            IrAlSiguientePunto();
        }
    }


    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (jugador == null)
            return;

        if (agente == null)
            return;

        if (!agente.enabled)
            return;

        if (!agente.isOnNavMesh)
            return;


        float distancia =
            Vector3.Distance(
                transform.position,
                jugador.position
            );

        bool jugadorVisible =
            JugadorVisible();


        // ========================================================
        // 1. SALUD BAJA → HUIR
        // ========================================================

        if (salud <= saludMinimaParaHuir &&
            salud > 0f)
        {
            estadoActual = Estado.Huir;
        }


        // ========================================================
        // 2. EN RANGO DE ATAQUE
        // ========================================================

        else if (distancia <= distanciaAtaque &&
                 jugadorVisible)
        {
            estadoActual = Estado.Atacar;

            ultimoPuntoVisto =
                jugador.position;
        }


        // ========================================================
        // 3. JUGADOR VISIBLE
        // ========================================================

        else if (jugadorVisible)
        {
            estadoActual = Estado.Perseguir;

            ultimoPuntoVisto =
                jugador.position;
        }


        // ========================================================
        // 4. JUGADOR PERDIDO
        // ========================================================

        else if (
            estadoActual == Estado.Perseguir ||
            estadoActual == Estado.Atacar)
        {
            estadoActual = Estado.Buscar;

            temporizadorBusqueda =
                tiempoBusqueda;
        }


        // ========================================================
        // EJECUTAR ESTADO
        // ========================================================

        switch (estadoActual)
        {
            case Estado.Patrullar:
                Patrullar();
                break;

            case Estado.Perseguir:
                Perseguir();
                break;

            case Estado.Atacar:
                Atacar();
                break;

            case Estado.Buscar:
                Buscar();
                break;

            case Estado.Huir:
                Huir();
                break;
        }


        // Mantener Z en 0
        Vector3 posicion =
            transform.position;

        posicion.z = 0f;

        transform.position =
            posicion;
    }


    // ============================================================
    // PATRULLAR
    // ============================================================

    void Patrullar()
    {
        if (puntosPatrulla == null ||
            puntosPatrulla.Length == 0)
            return;

        if (!agente.pathPending &&
            agente.remainingDistance <= 0.5f)
        {
            IrAlSiguientePunto();
        }
    }


    void IrAlSiguientePunto()
    {
        if (puntosPatrulla == null ||
            puntosPatrulla.Length == 0)
            return;

        if (!agente.isOnNavMesh)
            return;

        if (puntosPatrulla[puntoActual] == null)
            return;

        agente.SetDestination(
            puntosPatrulla[puntoActual].position
        );

        puntoActual =
            (puntoActual + 1) %
            puntosPatrulla.Length;
    }


    // ============================================================
    // PERSEGUIR
    // ============================================================

    void Perseguir()
    {
        if (jugador == null)
            return;

        agente.isStopped = false;

        agente.SetDestination(
            jugador.position
        );
    }


    // ============================================================
    // ATACAR
    // ============================================================

    void Atacar()
    {
        if (jugador == null)
            return;

        agente.ResetPath();

        agente.isStopped = true;

        temporizadorAtaque -=
            Time.deltaTime;

        if (temporizadorAtaque <= 0f)
        {
            Debug.Log(
                "Enemy atacó al NPC."
            );

            // Buscar el UtilityAgent del NPC
            UtilityAgent npc =
                jugador.GetComponent<UtilityAgent>();

            if (npc != null)
            {
                npc.RecibirDaño(
                    dañoAtaque
                );
            }

            temporizadorAtaque =
                tiempoEntreAtaques;
        }
    }


    // ============================================================
    // BUSCAR
    // ============================================================

    void Buscar()
    {
        if (!agente.isOnNavMesh)
            return;

        agente.isStopped = false;

        agente.SetDestination(
            ultimoPuntoVisto
        );

        if (!agente.pathPending &&
            agente.remainingDistance <=
            distanciaLlegadaBusqueda)
        {
            temporizadorBusqueda -=
                Time.deltaTime;

            if (temporizadorBusqueda <= 0f)
            {
                Debug.Log(
                    "Enemy no encontró al NPC. " +
                    "Regresando a patrullar."
                );

                estadoActual =
                    Estado.Patrullar;

                IrAlSiguientePunto();
            }
        }
    }


    // ============================================================
    // RECIBIR DAÑO
    // ============================================================

    public void RecibirDaño(float cantidadDaño)
    {
        if (salud <= 0f)
            return;

        salud -= cantidadDaño;

        salud =
            Mathf.Clamp(
                salud,
                0f,
                100f
            );

        Debug.Log(
            "Enemy recibió " +
            cantidadDaño +
            " de daño. Salud restante: " +
            salud
        );

        if (salud <= 0f)
        {
            Debug.Log(
                "El Enemy ha muerto."
            );

            if (agente != null &&
                agente.enabled &&
                agente.isOnNavMesh)
            {
                agente.ResetPath();
                agente.isStopped = true;
            }

            // Destruir el Enemy
            Destroy(gameObject);
        }
    }


    // ============================================================
    // HUIR
    // ============================================================

    void Huir()
    {
        if (jugador == null)
            return;

        if (!agente.isOnNavMesh)
            return;


        Vector3 direccionHuir =
            transform.position -
            jugador.position;

        direccionHuir.z = 0f;


        if (direccionHuir.sqrMagnitude < 0.01f)
        {
            direccionHuir =
                Vector3.right;
        }


        direccionHuir.Normalize();


        Vector3 destinoDeseado =
            transform.position +
            direccionHuir * 10f;


        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            destinoDeseado,
            out hit,
            5f,
            NavMesh.AllAreas))
        {
            agente.isStopped = false;

            agente.SetDestination(
                hit.position
            );
        }
    }


    // ============================================================
    // VISIBILIDAD
    // ============================================================

    bool JugadorVisible()
    {
        if (jugador == null)
            return false;


        Vector3 direccion =
            jugador.position -
            transform.position;

        float distancia =
            direccion.magnitude;


        if (distancia > distanciaVision)
            return false;


        if (distancia <= 0.01f)
            return true;


        direccion.Normalize();


        RaycastHit2D golpe =
            Physics2D.Raycast(
                transform.position,
                direccion,
                distancia,
                capasObstaculos
            );


        return golpe.collider == null;
    }
}