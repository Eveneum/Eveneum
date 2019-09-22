function WriteEvents(events) {
    var context = getContext();
    var container = context.getCollection();
    var containerLink = container.getSelfLink();

    // The count of imported events, also used as current item index.
    var count = 0;

    // Validate input.
    if (!events) throw new Error("The array is undefined or null.");

    var itemsLength = events.length;
    if (itemsLength === 0) {
        context.getResponse().setBody(0);
    }

    tryCreate(events[count], callback);

    // Note that there are 2 exit conditions:
    // 1) The createDocument request was not accepted.
    //    In this case the callback will not be called, we just call setBody and we are done.
    // 2) The callback was called events.length times.
    //    In this case all events were created and we don’t need to call tryCreate anymore. Just call setBody and we are done.
    function tryCreate(item, callback) {
        var isAccepted = container.createDocument(containerLink, item, callback);

        // If the request was accepted, callback will be called.
        // Otherwise report current count back to the client, which will call the script again with remaining set of events.
        if (!isAccepted) context.getResponse().setBody(count);
    }

    // This is called when container.createDocument is done in order to process the result.
    function callback(err, item, options) {
        if (err) throw err;

        // One more event has been inserted, increment the count.
        count++;

        if (count >= itemsLength) {
            // If we created all events, we are done. Just set the response.
            context.getResponse().setBody(count);
        } else {
            // Create next document.
            tryCreate(events[count], callback);
        }
    }
}