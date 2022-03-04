import React, { Component } from 'react';

class QuestionAccordion extends Component {
    render() {
        return (
            <div className="accordion">
            <button className="accordion__btn js-open-accordion">
                <span className="font-ico-chevron-down"></span> {this.props.title} </button>
            <div className="accordion__content">
                {this.props.description.split('<br/>').map((descriptionText, index) => <p key={index}>{descriptionText}</p>)}
            </div>
        </div>
        );
    }
}

export default QuestionAccordion;