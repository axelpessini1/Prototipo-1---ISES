# Python/api_juego.py

class GameAPI:
    def __init__(self):
        self.commands = []

    def moveRight(self, cantidad=1):
        self.commands.append({
            "action": "move",
            "direction": "right",
            "amount": cantidad
        })

    def moveLeft(self, cantidad=1):
        self.commands.append({
            "action": "move",
            "direction": "left",
            "amount": cantidad
        })

    def moveUp(self, cantidad=1):
        self.commands.append({
            "action": "move",
            "direction": "up",
            "amount": cantidad
        })

    def moveDown(self, cantidad=1):
        self.commands.append({
            "action": "move",
            "direction": "down",
            "amount": cantidad
        })

    def collect(self):
        self.commands.append({
            "action": "collect"
        })

    def get_commands(self):
        return self.commands