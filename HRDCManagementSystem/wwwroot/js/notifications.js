/**
 * notifications.js - Client-side functionality for real-time notifications
 * Uses SignalR to connect to NotificationHub
 */

// Notification connection and handling
(function () {
    'use strict';

    // Reference to the SignalR connection
    let connection = null;

    // Initialize SignalR connection to notification hub
    function initializeSignalR() {
        try {
            // Create the connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect()
                .build();

            // Handler for receiving new notifications
            connection.on("ReceiveNotification", function (notification) {
                console.log("Notification received:", notification);
                
                // Show notification in UI
                showNotification(notification);
                
                // Update notification counter
                updateNotificationCounter(1);
            });

            // Handler for when a notification is marked as read
            connection.on("NotificationRead", function (notificationId) {
                console.log("Notification marked as read:", notificationId);
                
                // Remove notification from UI if present
                removeNotificationFromUI(notificationId);
                
                // Update notification counter
                updateNotificationCounter(-1);
            });

            // Handler for when all notifications are marked as read
            connection.on("AllNotificationsRead", function () {
                console.log("All notifications marked as read");
                
                // Clear all notifications from UI
                clearNotifications();
                
                // Reset notification counter
                resetNotificationCounter();
            });

            // Start the connection
            connection.start()
                .then(() => {
                    console.log("SignalR connection established successfully.");
                })
                .catch(err => {
                    console.error("Error while establishing SignalR connection:", err);
                });
        } catch (err) {
            console.error("Error initializing SignalR:", err);
        }
    }

    // Show a notification in the UI
    function showNotification(notification) {
        // Add to notification dropdown if it exists
        const notificationList = document.getElementById('notificationList');
        if (notificationList) {
            const notificationItem = document.createElement('a');
            notificationItem.href = '#';
            notificationItem.className = 'dropdown-item notification-item d-flex align-items-center py-2';
            notificationItem.dataset.id = notification.id;
            
            notificationItem.innerHTML = `
                <div class="notification-icon me-3">
                    <span class="badge bg-primary p-2">
                        <i class="bi bi-bell-fill"></i>
                    </span>
                </div>
                <div class="notification-content flex-grow-1">
                    <div class="fw-semibold">${notification.title}</div>
                    <div class="small text-muted">${notification.message}</div>
                </div>
            `;
            
            // Add click handler to mark as read
            notificationItem.addEventListener('click', function(e) {
                e.preventDefault();
                markNotificationAsRead(notification.id);
            });
            
            // Add to the list (at the top)
            if (notificationList.firstChild) {
                notificationList.insertBefore(notificationItem, notificationList.firstChild);
            } else {
                notificationList.appendChild(notificationItem);
            }
            
            // Remove any "no notifications" message
            const noNotifications = document.getElementById('noNotifications');
            if (noNotifications) {
                noNotifications.remove();
            }
        }

        // Show toast notification if enabled
        showToastNotification(notification.title, notification.message);
    }

    // Show a toast notification
    function showToastNotification(title, message) {
        // Check if browser supports notifications
        if (!("Notification" in window)) {
            return;
        }

        // Check if the user has already granted permission
        if (Notification.permission === "granted") {
            const notification = new Notification(title, {
                body: message,
                icon: "/images/hrdc-logo.png"
            });
            
            // Close notification after 5 seconds
            setTimeout(notification.close.bind(notification), 5000);
        }
        // Otherwise, request permission if not denied
        else if (Notification.permission !== "denied") {
            Notification.requestPermission().then(function (permission) {
                if (permission === "granted") {
                    const notification = new Notification(title, {
                        body: message,
                        icon: "/images/hrdc-logo.png"
                    });
                    
                    // Close notification after 5 seconds
                    setTimeout(notification.close.bind(notification), 5000);
                }
            });
        }
    }

    // Remove a notification from the UI
    function removeNotificationFromUI(notificationId) {
        const notificationItem = document.querySelector(`.notification-item[data-id="${notificationId}"]`);
        if (notificationItem) {
            notificationItem.remove();
            
            // Add "no notifications" message if list is empty
            const notificationList = document.getElementById('notificationList');
            if (notificationList && notificationList.children.length === 0) {
                const noNotificationsMessage = document.createElement('div');
                noNotificationsMessage.id = 'noNotifications';
                noNotificationsMessage.className = 'text-center py-3 text-muted';
                noNotificationsMessage.innerText = 'No new notifications';
                notificationList.appendChild(noNotificationsMessage);
            }
        }
    }

    // Clear all notifications from the UI
    function clearNotifications() {
        const notificationList = document.getElementById('notificationList');
        if (notificationList) {
            // Clear all notification items
            notificationList.innerHTML = '';
            
            // Add "no notifications" message
            const noNotificationsMessage = document.createElement('div');
            noNotificationsMessage.id = 'noNotifications';
            noNotificationsMessage.className = 'text-center py-3 text-muted';
            noNotificationsMessage.innerText = 'No new notifications';
            notificationList.appendChild(noNotificationsMessage);
        }
    }

    // Update notification counter (add or subtract)
    function updateNotificationCounter(delta) {
        const notificationBadge = document.getElementById('notificationBadge');
        if (notificationBadge) {
            let count = parseInt(notificationBadge.innerText || '0', 10);
            count += delta;
            
            if (count <= 0) {
                notificationBadge.style.display = 'none';
                notificationBadge.innerText = '0';
            } else {
                notificationBadge.style.display = '';
                notificationBadge.innerText = count.toString();
            }
        }
    }

    // Reset notification counter to zero
    function resetNotificationCounter() {
        const notificationBadge = document.getElementById('notificationBadge');
        if (notificationBadge) {
            notificationBadge.style.display = 'none';
            notificationBadge.innerText = '0';
        }
    }

    // Mark a notification as read
    function markNotificationAsRead(notificationId) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        
        // Create form data
        const formData = new FormData();
        formData.append('notificationId', notificationId);
        
        // Add CSRF token if available
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }
        
        // Send request to mark as read
        fetch('/Notification/MarkAsRead', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            // This will be handled by the SignalR event
        })
        .catch(error => {
            console.error('Error marking notification as read:', error);
        });
    }

    // Mark all notifications as read
    function markAllAsRead() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        
        // Create form data
        const formData = new FormData();
        
        // Add CSRF token if available
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }
        
        // Send request to mark all as read
        fetch('/Notification/MarkAllAsRead', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            // This will be handled by the SignalR event
        })
        .catch(error => {
            console.error('Error marking all notifications as read:', error);
        });
    }

    // Initialize when document is ready
    document.addEventListener('DOMContentLoaded', function() {
        // Initialize SignalR
        initializeSignalR();
        
        // Request notification permission
        if ("Notification" in window && Notification.permission !== "granted" && Notification.permission !== "denied") {
            Notification.requestPermission();
        }
        
        // Add click event for "Mark All as Read" button
        const markAllAsReadBtn = document.getElementById('markAllAsReadBtn');
        if (markAllAsReadBtn) {
            markAllAsReadBtn.addEventListener('click', function(e) {
                e.preventDefault();
                markAllAsRead();
            });
        }
    });

    // Export functions to global scope if needed
    window.NotificationClient = {
        markNotificationAsRead: markNotificationAsRead,
        markAllAsRead: markAllAsRead
    };
})();