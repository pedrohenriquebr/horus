document.addEventListener('DOMContentLoaded', function () {
    // Toggle sidebar
    const sidebar = document.getElementById('sidebar');
    const menuToggle = document.getElementById('menuToggle');

    // Initially hide sidebar on small screens
    if (window.innerWidth < 1024) {
        sidebar.classList.add('-translate-x-full');
    }

    menuToggle.addEventListener('click', function () {
        console.log('Toggle clicked');
        sidebar.classList.toggle('-translate-x-full');
    });

    // Message input handling
    const messageInput = document.getElementById('message-input');
    const sendButton = messageInput.nextElementSibling;

    const sendMessage = () => {
        const text = messageInput.value.trim();
        if (text) {
            console.log('Sending message:', text);
            messageInput.value = '';
        }
    };

    messageInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    sendButton.addEventListener('click', sendMessage);

    // Quick action buttons
    document.querySelectorAll('.quick-action-btn').forEach(button => {
        button.addEventListener('click', function () {
            const action = this.textContent.trim();
            messageInput.value = `Help me ${action.toLowerCase()}`;
            messageInput.focus();
        });
    });
});
