Feature: Telegram Bot Service
    
    Telegram Bot Service is responsible for the sending and receiving 
    messages from telegram bot and initiating processing with other
    services.
    
Scenario: Telegram Bot Service reports healthy status when running.
    Given the application is started
    When HTTP GET /health request sent to the telegram-bot service
    Then Response status is 200