# Technical Plan: High Availability & Redundancy with Master-Slave Mesh Architecture

## Overview

Implement a high-availability distributed key-value store with master-slave mesh topology, where each node maintains a main-replica pair, and clients dynamically discover and connect to the cluster through a master node.

---

## Change Summary

### Architecture Components

1. **Node Types & Roles**

   - **Master Node**: First server in cluster, coordinates node registration and topology propagation
   - **Slave Nodes**: All subsequent nodes, register with master and receive cluster topology
   - **Main Server**: Primary server instance for each node
   - **Replica Server**: Backup server instance paired with each main server
   - 
![Cluster_Topology.png](../../Documents/Screenshot%202025-12-28%20at%2011.03.46%E2%80%AFPM.png)
2. **Replication Strategy**

   - Each write operation (PUT/PATCH) writes to both main and replica server
   - Writes must succeed on both before returning success to client
   - Read operations can read from either main or replica (prefer main, fallback to replica)

3. **Cluster Discovery & Topology**

   - New nodes register with master node on startup
   - Master propagates node information to all existing nodes
   - All nodes maintain connection information map of all other nodes
   - No active connections between nodes (only connection metadata)

4. **Client Initialization Flow**

   - Client connects to any known server
   - Server responds with master node information
   - Client connects to master node
   - Master provides full cluster topology (all nodes + their replicas)
   - Client stores topology map for routing operations

5. **Health Monitoring**
   - Client periodically health checks all known servers
   - Failed servers marked as inactive
   - Operations automatically redirected to healthy nodes
   - Health check includes both main and replica servers

---

## Expected Benefits

- **High Availability**: Data replicated at node level (main + replica), survives single server failures
- **Fault Tolerance**: Automatic failover when servers become unavailable
- **Dynamic Cluster**: Nodes can join/leave without manual client reconfiguration
- **Self-Discovery**: Clients automatically discover full cluster topology
- **Redundancy**: Multiple layers of redundancy (node-level replication + multiple nodes)
- **Simplified Client Setup**: Client only needs to know one server initially

---

## Important Tradeoffs

### Consistency & Durability

- **Write Latency**: Writes must complete on both main and replica (2x network round-trips)
- **Write Failure Handling**: If replica write fails, need strategy (fail entire write? continue with main only?)
- **Read Consistency**: Reading from replica may return slightly stale data if replication lag exists

### Master Node Dependency

- **Single Point of Failure**: Master node failure requires election mechanism or manual promotion
- **Master Bottleneck**: All new node registrations go through master
- **Master Recovery**: Need strategy for master node recovery/restart

### Network & Performance

- **Topology Propagation**: Broadcasting node changes to all nodes creates O(n) network messages
- **Health Check Overhead**: Periodic health checks from all clients create network load
- **No Inter-Node Communication**: Nodes don't actively communicate (only metadata), may lead to stale views

### Operational Complexity

- **Replica Synchronization**: Need to handle replica lag and catch-up scenarios
- **Split-Brain Scenarios**: If master and slaves lose connectivity, need resolution strategy
- **Node Removal**: Graceful removal of nodes from cluster topology

---

## Rough Effort & Sequence

### Phase 1: Server-Side Replication Infrastructure (3-4 weeks)

- Extend configuration models to support replica servers (main + replica pairs)
- Create `ReplicatedStore` wrapper that writes to both main and replica stores
- Implement write coordination and failure handling
- Add read strategy with main/replica fallback

**Deliverables:** Replicated store, configuration models, write/read replication logic

---

### Phase 2: Master-Slave Node Roles (2-3 weeks)

- Implement node role detection (first node = master, others = slaves)
- Create master node services: registration endpoint, master info endpoint, topology endpoint
- Implement slave node services: master discovery on startup, registration with master

**Deliverables:** Master/slave role management, node registration API, master discovery

---

### Phase 3: Cluster Topology Management (3-4 weeks)

- Create topology data models (`ClusterTopology`, `NodeInfo`, `TopologyUpdate`)
- Implement in-memory topology storage per node
- Implement topology broadcast from master to all slaves
- Handle concurrent node registrations and topology synchronization

**Deliverables:** Topology models, storage, propagation, and synchronization

---

### Phase 4: Client-Side Discovery & Routing (3-4 weeks)

- Implement client discovery flow: bootstrap server → master → topology
- Create `ClusterTopologyManager` for dynamic topology management
- Update `StoreHttpClientFactory` to use dynamic topology for routing
- Add bootstrap server configuration (backward compatible with static config)

**Deliverables:** Client discovery, dynamic topology management, updated routing

---

### Phase 5: Health Monitoring & Failover (2-3 weeks)

- Add health check endpoint (`GET /health`) on servers
- Implement client-side periodic health monitoring (configurable interval)
- Add automatic failover: exclude unhealthy servers, route to healthy nodes/replicas
- Track health status and recovery detection

**Deliverables:** Health check endpoints, client monitoring, automatic failover

---

### Phase 6: Replication Failure Handling (2-3 weeks)

- Handle write failure scenarios (main/replica success/failure combinations)
- Implement replica synchronization and catch-up mechanisms
- Add consistency guarantees and split-brain prevention

**Deliverables:** Replication failure handling, replica sync, consistency guarantees

---

### Phase 7: Master Node Resilience (2-3 weeks)

- Handle master node restart and recovery
- Implement topology reconstruction on master recovery
- (Optional) Basic master election protocol for future enhancement

**Deliverables:** Master failure handling, recovery mechanisms

---

### Phase 8: Testing & Integration (2-3 weeks)

- Integration tests: cluster initialization, registration, discovery, failover
- Failure scenario tests: node failures, network partitions, concurrent operations
- Performance testing: replication overhead, health check impact, load testing
- Documentation updates

**Deliverables:** Test suite, performance benchmarks, updated documentation

---

## Total Estimated Effort: **19-28 weeks** (4.5-7 months)

_Note: Effort estimates assume 1-2 developers. Can be parallelized across phases 1-3 and 4-5._

---

### Key Design Decisions Needed

1. **Write Consistency**: Strict (both main+replica must succeed) vs. Relaxed (main success is enough)
2. **Master Election**: Implement now or defer? If defer, what happens on master failure?
3. **Topology Persistence**: Store topology on disk for master recovery, or rebuild on restart?
4. **Replica Sync**: Real-time sync or eventual consistency with catch-up?
5. **Health Check Frequency**: Configurable per deployment (30s default reasonable?)

---

## Migration Path

1. **Backward Compatibility**: Existing static configuration continues to work
2. **Gradual Rollout**: Can deploy with replication disabled initially
3. **Feature Flags**: Add configuration flags to enable/disable replication, master-slave mode
4. **Monitoring**: Add metrics for replication lag, health check failures, topology updates

---
