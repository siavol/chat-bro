Feature: Telegram Bot Service
    
    Telegram Bot capability is hosted inside chatbro-server and is responsible
    for sending and receiving messages from telegram while coordinating with
    other services.
    
Scenario: Telegram Bot Service reports healthy status when running.
    Given the application is started
    When I send HTTP request to the chatbro-server service
    And request is GET /health
    Then the response status is 200

    
#Scenario: Telegram Bot responds to the messages in the telegram
#    Given the application is started
#    When telegram user sends a message "Hey bro!"
#    Then bot responds in telegram with "said: hey bro."
