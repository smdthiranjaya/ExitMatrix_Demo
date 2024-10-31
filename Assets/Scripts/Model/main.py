from flask import Flask, request
from flask_restx import Api, Resource, fields
import json
import heapq
from typing import List, Tuple, Dict
import logging
import time


app = Flask(__name__)
api = Api(app, version='1.0', title='Building Navigation API',
          description='API for finding safe paths in a building')

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class BuildingMap:
    def __init__(self, json_file: str):
        with open(json_file, 'r') as f:
            data = json.load(f)
        self.floors = data['Floors']
        self.graph = self._create_graph()
        self.min_x = float('inf')
        self.min_z = float('inf')
        self.max_x = float('-inf')
        self.max_z = float('-inf')
        self._calculate_bounds()

    def _calculate_bounds(self):
        for floor in self.floors:
            for room in floor['Rooms']:
                x, _, z = room['Position'].values()
                width, _, depth = room['Size'].values()
                self.min_x = min(self.min_x, x - width/2)
                self.max_x = max(self.max_x, x + width/2)
                self.min_z = min(self.min_z, z - depth/2)
                self.max_z = max(self.max_z, z + depth/2)

    def _create_graph(self) -> Dict[Tuple[int, int, int], List[Tuple[int, int, int]]]:
        graph = {}
        for floor in self.floors:
            floor_num = floor['Floor_Number']
            map_2d = floor['2D_Map']
            for i in range(len(map_2d)):
                for j in range(len(map_2d[i])):
                    if map_2d[i][j] != '#':  # If not a wall
                        node = (floor_num, i, j)
                        graph[node] = []
                        # Check neighbors
                        for di, dj in [(0, 1), (1, 0), (0, -1), (-1, 0)]:
                            ni, nj = i + di, j + dj
                            if 0 <= ni < len(map_2d) and 0 <= nj < len(map_2d[i]) and map_2d[ni][nj] != '#':
                                graph[node].append((floor_num, ni, nj))
                        # Check for stairs/exits
                        if map_2d[i][j] == 'E':
                            for other_floor in self.floors:
                                if other_floor['Floor_Number'] != floor_num:
                                    other_map = other_floor['2D_Map']
                                    if other_map[i][j] == 'E':
                                        graph[node].append((other_floor['Floor_Number'], i, j))
        return graph

    def find_path(self, start: Tuple[int, int, int], end: Tuple[int, int, int], fire_positions: List[Tuple[int, int, int]] = None) -> List[Tuple[int, int, int]]:
        if start not in self.graph:
            closest_start = self._find_closest_valid_point(start)
            logger.warning(f"Start position {start} not in graph. Using closest valid point: {closest_start}")
            start = closest_start

        if end not in self.graph:
            closest_end = self._find_closest_valid_point(end)
            logger.warning(f"End position {end} not in graph. Using closest valid point: {closest_end}")
            end = closest_end

        def heuristic(a, b):
            return abs(a[1] - b[1]) + abs(a[2] - b[2]) + abs(a[0] - b[0]) * 10

        if fire_positions is None:
            fire_positions = []

        def is_safe(pos):
            return all(abs(pos[0] - f[0]) + abs(pos[1] - f[1]) + abs(pos[2] - f[2]) > 2 for f in fire_positions)

        heap = [(0, start)]
        came_from = {}
        cost_so_far = {start: 0}

        while heap:
            current = heapq.heappop(heap)[1]

            if current == end:
                path = []
                while current in came_from:
                    path.append(current)
                    current = came_from[current]
                path.append(start)
                return path[::-1]

            for next in self.graph[current]:
                if not is_safe(next):
                    continue
                new_cost = cost_so_far[current] + 1
                if next not in cost_so_far or new_cost < cost_so_far[next]:
                    cost_so_far[next] = new_cost
                    priority = new_cost + heuristic(end, next)
                    heapq.heappush(heap, (priority, next))
                    came_from[next] = current

        return []  # No path found

    def _find_closest_valid_point(self, point: Tuple[int, int, int]) -> Tuple[int, int, int]:
        floor, x, z = point
        x = max(self.min_x, min(self.max_x, x))
        z = max(self.min_z, min(self.max_z, z))
        closest_point = min(self.graph.keys(), key=lambda p: ((p[1]-x)**2 + (p[2]-z)**2) if p[0] == floor else float('inf'))
        return closest_point
    
def generate_consolidated_instructions(path: List[Tuple[int, int, int]], grid_size: float = 0.1) -> List[str]:
    instructions = []
    current_direction = None
    distance = 0
    
    for i in range(1, len(path)):
        prev, curr = path[i-1], path[i]
        
        if prev[0] != curr[0]:
            if current_direction:
                instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
            if curr[0] > prev[0]:
                instructions.append(f"Go up to floor {curr[0]}")
            else:
                instructions.append(f"Go down to floor {curr[0]}")
            current_direction = None
            distance = 0
        elif prev[1] < curr[1]:
            if current_direction != "forward":
                if current_direction:
                    instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
                current_direction = "forward"
                distance = 0
            distance += 1
        elif prev[1] > curr[1]:
            if current_direction != "backward":
                if current_direction:
                    instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
                current_direction = "backward"
                distance = 0
            distance += 1
        elif prev[2] < curr[2]:
            if current_direction != "right":
                if current_direction:
                    instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
                current_direction = "right"
                distance = 0
            distance += 1
        elif prev[2] > curr[2]:
            if current_direction != "left":
                if current_direction:
                    instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
                current_direction = "left"
                distance = 0
            distance += 1
    
    if current_direction:
        instructions.append(f"Move {current_direction} {distance * grid_size:.1f} meters")
    
    return instructions

building_map = BuildingMap('building_map.json')

position_model = api.model('Position', {
    'x': fields.String(required=True, description='X coordinate as string'),
    'y': fields.String(required=True, description='Y coordinate as string'),
    'z': fields.String(required=True, description='Z coordinate as string')
})

input_model = api.model('NavigationInput', {
    'current_floor': fields.Integer(required=True, description='Current floor number'),
    'current_room': fields.String(required=True, description='Current room name'),
    'player_position': fields.Nested(position_model, required=True, description='Player position'),
    'fire_positions': fields.List(fields.Nested(position_model), required=False, description='List of fire positions')
})


@api.route('/navigate')
class Navigate(Resource):
    @api.expect(input_model)
    def post(self):
        start_time = time.time()
        logger.info("Received navigation request")
        
        data = request.json
        current_floor = data['current_floor']
        current_room = data['current_room']
        player_pos = data['player_position']
        fire_positions = data.get('fire_positions', [])

        logger.info(f"Current floor: {current_floor}, Current room: {current_room}")
        logger.info(f"Player position: {player_pos}")
        logger.info(f"Fire positions: {fire_positions}")

        try:
            # Convert string coordinates to float
            player_pos_float = {k: float(v) for k, v in player_pos.items()}

            # Convert player position to the format expected by find_path
            start = (current_floor, int(player_pos_float['z'] * 10), int(player_pos_float['x'] * 10))

            # Find the nearest exit
            exit_position = find_nearest_exit(current_floor)
            logger.info(f"Nearest exit found at: {exit_position}")

            # Convert fire positions to the format expected by find_path
            fire_pos_converted = [
                (current_floor, int(float(pos['z']) * 10), int(float(pos['x']) * 10))
                for pos in fire_positions
            ]

            logger.info("Starting pathfinding algorithm")
            path = building_map.find_path(start, exit_position, fire_pos_converted)
            if not path:
                logger.warning("No safe path found")
                return {'error': 'No safe path found'}, 404

            logger.info(f"Path found: {path}")

            instructions = generate_consolidated_instructions(path)
            logger.info(f"Generated instructions: {instructions}")

            end_time = time.time()
            logger.info(f"Request processed in {end_time - start_time:.2f} seconds")

            return {
                'path': path,
                'instructions': instructions,
                'exit_position': exit_position
            }

        except Exception as e:
            logger.error(f"An error occurred: {str(e)}")
            return {'error': str(e)}, 500
        
def find_nearest_exit(floor):
    # This function should find the nearest exit on the given floor
    # For now, we'll just return a dummy exit position
    return (floor, 90, 90)  # Assuming the exit is at position (9, 9) on the current floor

if __name__ == '__main__':
    app.run(debug=True)