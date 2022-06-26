using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using UnityEditor.UI;

public class FlowField : MonoBehaviour
{
    FastNoise _fastNoise;
    public Vector3Int _gridSize;

    // set cell size
    public float _cellSize;

    //create a 3 dimensional vector
    public Vector3[,,] _flowfieldDirection;

    // resolution of the noise applied to the grid
    public float _increment;

    public Vector3 _offset, _offsetspeed;

    // patricles
    public GameObject _particlePrefab;
    public int _amountOfParticles;
    [HideInInspector]
    public List<FlowFieldParticle> _particles;
    public float _particleScale;
    public float _spawnRadius, _particleMoveSpeed, _particleRotateSpeed;

    // plexus related
    public float maxDistance = 1.0f;

    public int maxConnections = 5;
    public int maxLineRenderers = 100;

    public LineRenderer lineRendererTemplate;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();

    Transform _transform;

    bool _particleSpawnVaqlidation(Vector3 position)
    {
        bool valid = true;
        foreach(FlowFieldParticle particle in _particles)
        {
            if(Vector3.Distance(position, particle.transform.position) < _spawnRadius)
            {
                valid = false; 
                break;
            }
        }
        if(valid)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        _flowfieldDirection = new Vector3[_gridSize.x, _gridSize.y, _gridSize.z];

        // instanciate fast noise
        _fastNoise = new FastNoise();

        _particles = new List<FlowFieldParticle>();
        for (int i = 0; i < _amountOfParticles; i++)
        {
            int attempt = 0;

            while (attempt < 100)
            {
                //giving particles random position
                Vector3 randomPos = new Vector3(
                Random.Range(this.transform.position.x, this.transform.position.x + _gridSize.x * _cellSize),
                Random.Range(this.transform.position.y, this.transform.position.y + _gridSize.y * _cellSize),
                Random.Range(this.transform.position.z, this.transform.position.z + _gridSize.z * _cellSize));
                // prevents overlapping
                bool isValid = _particleSpawnVaqlidation(randomPos);
                if (isValid)
                {
                    GameObject particleInstance = (GameObject)Instantiate(_particlePrefab);
                    particleInstance.transform.position = randomPos;
                    particleInstance.transform.parent = this.transform;
                    particleInstance.transform.localScale = new Vector3(_particleScale, _particleScale, _particleScale);
                    _particles.Add(particleInstance.GetComponent<FlowFieldParticle>());
                    break;
                }
                if (!isValid)
                {
                    attempt++;
                }

            }
            
        }
        Debug.Log(_particles.Count);

        

    }

    // Update is called once per frame
    void Update()
    {
        CalculateFlowfieldDirections();
        ParticleBehaviour();
    }

    public void CalculateFlowfieldDirections()
    {

        _offset = new Vector3(_offset.x + (_offsetspeed.x * Time.deltaTime), _offset.y + (_offsetspeed.y * 
            Time.deltaTime), _offset.z + (_offsetspeed.z * Time.deltaTime));
        
        // create grid
        float xoff = 0f;
        for (int x = 0; x < _gridSize.x; x++)
        {
            float yoff = 0f;
            for (int y = 0; y < _gridSize.y; y++)
            {
                float zoff = 0f;
                for (int z = 0; z < _gridSize.z; z++)
                {
                    float noise = _fastNoise.GetSimplex(xoff + _offset.x, yoff + _offset.y, zoff + _offset.z) + 1;

                    //convert noise into a direction
                    Vector3 noiseDirection = new Vector3(Mathf.Cos(noise * Mathf.PI), Mathf.Sin(noise * Mathf.PI)
                        , Mathf.Cos(noise * Mathf.PI));

                    //instanciate particles and track positions of particles
                    _flowfieldDirection[x, y, z] = Vector3.Normalize(noiseDirection);

                    zoff += _increment;
                }
                yoff += _increment;
            }
            xoff += _increment;
        }

    }

    void ParticleBehaviour()
    {
        foreach(FlowFieldParticle p in _particles)
        {

            // if particle hits an edge of the box, spawn to the opposite site
            // X edges
            if(p.transform.position.x > this.transform.position.x + (_gridSize.x * _cellSize))
            {
                p.transform.position = new Vector3(this.transform.position.x, p.transform.position.y, p.transform.position.z);
            }
            if(p.transform.position.x < this.transform.position.x)
            {
                p.transform.position = new Vector3(this.transform.position.x + (_gridSize.x * _cellSize), p.transform.position.y, p.transform.position.z);
            }

            // Y edges
            if (p.transform.position.y > this.transform.position.y + (_gridSize.y * _cellSize))
            {
                p.transform.position = new Vector3( p.transform.position.x, this.transform.position.y, p.transform.position.z);
            }
            if (p.transform.position.y < this.transform.position.y)
            {
                p.transform.position = new Vector3(p.transform.position.x, this.transform.position.y + (_gridSize.y * _cellSize), p.transform.position.z);
            }

            // Z edges
            if (p.transform.position.z > this.transform.position.z + (_gridSize.z * _cellSize))
            {
                p.transform.position = new Vector3(p.transform.position.x, p.transform.position.y, this.transform.position.z);
            }
            if (p.transform.position.z < this.transform.position.z)
            {
                p.transform.position = new Vector3(p.transform.position.x, p.transform.position.y, this.transform.position.z + (_gridSize.z * _cellSize));
            }

            Vector3Int particlePos = new Vector3Int(
                Mathf.FloorToInt(Mathf.Clamp((p.transform.position.x - this.transform.position.x) / _cellSize, 0, _gridSize.x - 1)),
                Mathf.FloorToInt(Mathf.Clamp((p.transform.position.y - this.transform.position.y) / _cellSize, 0, _gridSize.y - 1)),
                Mathf.FloorToInt(Mathf.Clamp((p.transform.position.z - this.transform.position.z) / _cellSize, 0, _gridSize.z - 1))
                );

            // Move particles in the direction of the flow field
            p.ApplyRotation(_flowfieldDirection[particlePos.x, particlePos.y, particlePos.z], _particleRotateSpeed);
            p._moveSpeed = _particleMoveSpeed;
            p.transform.localScale = new Vector3(_particleScale, _particleScale, _particleScale);
        }
    }

    private void LateUpdate()
    {
        int lrIndex = 0;
        int lineRendererCount = lineRenderers.Count;

        if (lineRendererCount > maxLineRenderers)
        {
            for (int i = maxLineRenderers; i < lineRendererCount; i++)
            {
                Destroy(lineRenderers[i].gameObject);
            }

            int removedCount = lineRendererCount - maxLineRenderers;
            lineRenderers.RemoveRange(maxLineRenderers, removedCount);

            lineRendererCount -= removedCount;
        }

        if (maxConnections > 0 && maxLineRenderers > 0)
        {
            //loop through every single particle
            foreach (FlowFieldParticle p1 in _particles)
            {
 
                float maxDistanceSqr = maxDistance * maxDistance;

                _transform = transform;
                
                if (lrIndex == maxLineRenderers)
                {
                    break;
                }

                Vector3 p1_position = p1.transform.position;

                int connections = 0;

                foreach(FlowFieldParticle p2 in _particles)
                {
                    
                    Vector3 p2_position = p2.transform.localPosition;

                    //Get distances beetween p1 and p2, magnitude is lenght of the difference vector
                    float distanceSqr = Vector3.SqrMagnitude(p1_position - p2_position);

                    //if distance is less then max distance create a connection between particles
                    if (distanceSqr <= maxDistanceSqr)
                    {
                        LineRenderer lr;

                        // adding a new line Renderer
                        if (lrIndex == lineRendererCount)
                        {
                            lr = Instantiate(lineRendererTemplate, _transform, false);
                            lineRenderers.Add(lr);

                            lineRendererCount++;
                        }

                        lr = lineRenderers[lrIndex];

                        lr.enabled = true;
                        

                        lr.SetPosition(0, p1_position);
                        lr.SetPosition(1, p2_position);

                        lrIndex++;
                        connections++;

                        if (connections == maxConnections || lrIndex == maxLineRenderers)
                        {
                            break;
                        }
                        //Debug.Log("Made a connection:" +connections);

                    }
                }
            }
            //Disable unused LineRenderes, when count is less than actual lineRendererCount
            for (int i = lrIndex; i < lineRendererCount; i++)
            {
                lineRenderers[i].enabled = false;
            }
        }
    }


    private void OnDrawGizmos()
    {
        // everything that follows this line is white
        Gizmos.color = Color.white;

        Gizmos.DrawWireCube(this.transform.position + new Vector3((_gridSize.x * _cellSize) * 0.5f, (_gridSize.y * _cellSize) * 0.5f, (_gridSize.z * _cellSize) * 0.5f),
            new Vector3(_gridSize.x * _cellSize, _gridSize.y * _cellSize, _gridSize.z * _cellSize));
    }
}
